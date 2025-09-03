using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemoryPack.Generator;

public record TypeScriptGenerateOptions
{
    public string OutputDirectory { get; set; } = default!;
    public string ImportExtension { get; set; } = default!;
    public bool ConvertPropertyName { get; set; } = true;
    public bool EnableNullableTypes { get; set; } = false;
    public bool IsDesignTimeBuild { get; set; } = false;
}

partial class MemoryPackGenerator
{
    private static TypeMeta? GenerateTypeScript(TypeDeclarationSyntax syntax, Compilation compilation, TypeScriptGenerateOptions typeScriptGenerateOptions, in SourceProductionContext context,
        ReferenceSymbols reference, IReadOnlyDictionary<ITypeSymbol, ITypeSymbol> unionMap)
    {
        SemanticModel semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);

        INamedTypeSymbol? typeSymbol = semanticModel.GetDeclaredSymbol(syntax, context.CancellationToken);
        if (typeSymbol == null)
        {
            return null;
        }

        // require [MemoryPackable]
        if (!typeSymbol.ContainsAttribute(reference.MemoryPackableAttribute))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.GenerateTypeScriptMustBeMemoryPackable, syntax.Identifier.GetLocation(), typeSymbol.Name));
            return null;
        }

        var typeMeta = new TypeMeta(typeSymbol, reference);

        if (typeMeta.GenerateType is not (GenerateType.Object or GenerateType.Union))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.GenerateTypeScriptOnlyAllowsGenerateTypeObject, syntax.Identifier.GetLocation(), typeSymbol.Name));
            return null;
        }

        if (!Validate(typeMeta, syntax, context, reference))
        {
            return null;
        }

        var sb = new StringBuilder();

        sb.AppendLine($$"""
                        import { MemoryPackWriter } from "./MemoryPackWriter{{typeScriptGenerateOptions.ImportExtension}}";
                        import { MemoryPackReader } from "./MemoryPackReader{{typeScriptGenerateOptions.ImportExtension}}";
                        """);

        var collector = new TypeCollector();
        collector.Visit(typeMeta, true);

        // validate invalid enum
        foreach (ITypeSymbol? item in collector.GetEnums())
        {
            if (item.TypeKind == TypeKind.Enum && item is INamedTypeSymbol nts)
            {
                if (nts.EnumUnderlyingType!.SpecialType is SpecialType.System_Int64 or SpecialType.System_UInt64)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.GenerateTypeScriptDoesNotAllowLongEnum, syntax.Identifier.GetLocation(), typeSymbol.Name, item.FullyQualifiedToString()));
                    return null;
                }
            }
        }

        // add import(enum, union, memorypackable)
        foreach (ITypeSymbol? item in collector.GetEnums())
        {
            sb.AppendLine($"import {{ {item.Name} }} from \"./{item.Name}{typeScriptGenerateOptions.ImportExtension}\";");
        }

        foreach (ITypeSymbol? item in collector.GetMemoryPackableTypes(reference)
                                               .Where(x => !SymbolEqualityComparer.Default.Equals(x, typeSymbol) && !x.IsMemoryPackableNoGenerate(reference)))
        {
            sb.AppendLine($"import {{ {item.Name} }} from \"./{item.Name}{typeScriptGenerateOptions.ImportExtension}\";");
        }

        sb.AppendLine();

        try
        {
            typeMeta.EmitTypescript(sb, unionMap, typeScriptGenerateOptions);
        }
        catch (NotSupportedTypeException ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.GenerateTypeScriptNotSupportedType, ex.MemberMeta!.GetLocation(syntax),
                typeMeta.Symbol.Name, ex.MemberMeta.Name, ex.MemberMeta.MemberType.FullyQualifiedToString()));
            return null;
        }

        // save to file
        try
        {
            // if (!Directory.Exists(typeScriptGenerateOptions.OutputDirectory))
            // {
            //     Directory.CreateDirectory(typeScriptGenerateOptions.OutputDirectory);
            // }
            //
            // File.WriteAllText(Path.Combine(typeScriptGenerateOptions.OutputDirectory, $"{typeMeta.TypeName}.ts"), sb.ToString(), new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
        }

        return typeMeta;
    }

    private static void GenerateEnums(IEnumerable<ISymbol?>? enums, string typeScriptOutputDirectoryPath)
    {
        if (enums == null) return;
        // if (!Directory.Exists(typeScriptOutputDirectoryPath))
        // {
        //     Directory.CreateDirectory(typeScriptOutputDirectoryPath);
        // }

        foreach (ISymbol? e in enums)
        {
            if (e is INamedTypeSymbol typeSymbol)
            {
                if (typeSymbol.TypeKind != TypeKind.Enum) continue;

                var sb = new StringBuilder();
                foreach (ISymbol? member in typeSymbol.GetMembers())
                {
                    // (ok[0] as IFieldSymbol).ConstantValue
                    var fs = member as IFieldSymbol;
                    if (fs == null) continue;
                    string value = fs.HasConstantValue ? $" = {fs.ConstantValue}" : "";
                    sb.AppendLine($"    {fs.Name}{value},");
                }

                var code = $$"""
                             export const enum {{typeSymbol.Name}} {
                             {{sb}}
                             }
                             """;

                // File.WriteAllText(Path.Combine(typeScriptOutputDirectoryPath, $"{typeSymbol.Name}.ts"), code, new UTF8Encoding(false));
            }
        }
    }

    private static bool Validate(TypeMeta type, TypeDeclarationSyntax syntax, in SourceProductionContext context, ReferenceSymbols reference)
    {
        INamedTypeSymbol typeSymbol = type.Symbol;

        if (type.Symbol.IsGenericType)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.GenerateTypeScriptDoesNotAllowGenerics, syntax.Identifier.GetLocation(), typeSymbol.Name));
            return false;
        }

        foreach (MemberMeta item in type.Members)
        {
            if (item.Kind == MemberKind.CustomFormatter)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.GenerateTypeScriptNotSupportedCustomFormatter, item.GetLocation(syntax), typeSymbol.Name));
                return false;
            }
        }

        return true;
    }
}

public partial class TypeMeta
{
    public void EmitTypescript(StringBuilder sb, IReadOnlyDictionary<ITypeSymbol, ITypeSymbol> unionMap, TypeScriptGenerateOptions options)
    {
        string importExt = options.ImportExtension;
        if (this.IsUnion)
        {
            this.EmitTypeScriptUnion(sb, importExt);
            return;
        }

        if (!unionMap.TryGetValue(this.Symbol, out ITypeSymbol? union))
        {
            union = null;
        }

        TypeScriptMember[] tsMembers = this.Members.Select(x => new TypeScriptMember(x, this.reference, options)).ToArray();
        string impl = union != null ? $"implements {union.Name} " : "";

        var code = $$"""
                     export class {{this.TypeName}} {{impl}}{
                     {{this.EmitTypeScriptMembers(tsMembers)}}
                         constructor() {
                     {{this.EmitTypeScriptMembersInit(tsMembers)}}
                         }

                         static serialize(value: {{this.TypeName}} | null): Uint8Array {
                             const writer = MemoryPackWriter.getSharedInstance();
                             this.serializeCore(writer, value);
                             return writer.toArray();
                         }

                         static serializeCore(writer: MemoryPackWriter, value: {{this.TypeName}} | null): void {
                             if (value == null) {
                                 writer.writeNullObjectHeader();
                                 return;
                             }

                     {{this.EmitTypeScriptSerializeBody(tsMembers)}}
                         }

                         static serializeArray(value: ({{this.TypeName}} | null)[] | null): Uint8Array {
                             const writer = MemoryPackWriter.getSharedInstance();
                             this.serializeArrayCore(writer, value);
                             return writer.toArray();
                         }

                         static serializeArrayCore(writer: MemoryPackWriter, value: ({{this.TypeName}} | null)[] | null): void {
                             writer.writeArray(value, (writer, x) => {{this.TypeName}}.serializeCore(writer, x));
                         }

                         static deserialize(buffer: ArrayBuffer): {{this.TypeName}} | null {
                             return this.deserializeCore(new MemoryPackReader(buffer));
                         }

                         static deserializeCore(reader: MemoryPackReader): {{this.TypeName}} | null {
                             const [ok, count] = reader.tryReadObjectHeader();
                             if (!ok) {
                                 return null;
                             }

                             const value = new {{this.TypeName}}();
                             if (count == {{tsMembers.Length}}) {
                     {{this.EmitTypeScriptDeserializeBody(tsMembers, false)}}
                             }
                             else if (count > {{tsMembers.Length}}) {
                                 throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
                             }
                             else {
                     {{this.EmitTypeScriptDeserializeBody(tsMembers, true)}}
                             }
                             return value;
                         }

                         static deserializeArray(buffer: ArrayBuffer): ({{this.TypeName}} | null)[] | null {
                             return this.deserializeArrayCore(new MemoryPackReader(buffer));
                         }

                         static deserializeArrayCore(reader: MemoryPackReader): ({{this.TypeName}} | null)[] | null {
                             return reader.readArray(reader => {{this.TypeName}}.deserializeCore(reader));
                         }
                     }
                     """;

        sb.AppendLine(code);
    }

    public void EmitTypeScriptUnion(StringBuilder sb, string importExt)
    {
        string EmitUnionSerialize()
        {
            var sb = new StringBuilder();
            foreach ((ushort Tag, INamedTypeSymbol Type) item in this.UnionTags)
            {
                sb.AppendLine($$"""
                                        else if (value instanceof {{item.Type.Name}}) {
                                            writer.writeUnionHeader({{item.Tag}});
                                            {{item.Type.Name}}.serializeCore(writer, value);
                                            return;
                                        }
                                """);
            }

            return sb.ToString();
        }

        string EmitUnionDeserialize()
        {
            var sb = new StringBuilder();
            foreach ((ushort Tag, INamedTypeSymbol Type) item in this.UnionTags)
            {
                sb.AppendLine($$"""
                                            case {{item.Tag}}:
                                                return {{item.Type.Name}}.deserializeCore(reader);
                                """);
            }

            return sb.ToString();
        }

        foreach ((ushort Tag, INamedTypeSymbol Type) item in this.UnionTags)
        {
            sb.AppendLine($"import {{ {item.Type.Name} }} from \"./{item.Type.Name}{importExt}\"; ");
        }

        sb.AppendLine();

        var code = $$"""
                     export abstract class {{this.TypeName}} {
                         static serialize(value: {{this.TypeName}} | null): Uint8Array {
                             const writer = MemoryPackWriter.getSharedInstance();
                             this.serializeCore(writer, value);
                             return writer.toArray();
                         }

                         static serializeCore(writer: MemoryPackWriter, value: {{this.TypeName}} | null): void {
                             if (value == null) {
                                 writer.writeNullObjectHeader();
                                 return;
                             }
                     {{EmitUnionSerialize()}}
                             else {
                                 throw new Error("Concrete type is not in MemoryPackUnion");
                             }
                         }

                         static serializeArray(value: {{this.TypeName}}[] | null): Uint8Array {
                             const writer = MemoryPackWriter.getSharedInstance();
                             this.serializeArrayCore(writer, value);
                             return writer.toArray();
                         }

                         static serializeArrayCore(writer: MemoryPackWriter, value: {{this.TypeName}}[] | null): void {
                             writer.writeArray(value, (writer, x) => {{this.TypeName}}.serializeCore(writer, x));
                         }

                         static deserialize(buffer: ArrayBuffer): {{this.TypeName}} | null {
                             return this.deserializeCore(new MemoryPackReader(buffer));
                         }

                         static deserializeCore(reader: MemoryPackReader): {{this.TypeName}} | null {
                             const [ok, tag] = reader.tryReadUnionHeader();
                             if (!ok) {
                                 return null;
                             }

                             switch (tag) {
                     {{EmitUnionDeserialize()}}
                                 default:
                                     throw new Error("Tag is not found in this MemoryPackUnion");
                             }
                         }

                         static deserializeArray(buffer: ArrayBuffer): ({{this.TypeName}} | null)[] | null {
                             return this.deserializeArrayCore(new MemoryPackReader(buffer));
                         }

                         static deserializeArrayCore(reader: MemoryPackReader): ({{this.TypeName}} | null)[] | null {
                             return reader.readArray(reader => {{this.TypeName}}.deserializeCore(reader));
                         }
                     }
                     """;
        sb.AppendLine(code);
    }

    public string EmitTypeScriptMembers(TypeScriptMember[] members)
    {
        var sb = new StringBuilder();

        foreach (TypeScriptMember item in members)
        {
            sb.AppendLine($"    {item.MemberName}: {item.TypeName};");
        }

        return sb.ToString();
    }

    public string EmitTypeScriptMembersInit(TypeScriptMember[] members)
    {
        var sb = new StringBuilder();

        foreach (TypeScriptMember item in members)
        {
            sb.AppendLine($"        this.{item.MemberName} = {item.DefaultValue};");
        }

        return sb.ToString();
    }

    public string EmitTypeScriptSerializeBody(TypeScriptMember[] members)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"        writer.writeObjectHeader({members.Length});");
        foreach (TypeScriptMember item in members)
        {
            sb.AppendLine($"        {string.Format(item.WriteMethodTemplate, "value." + item.MemberName)};");
        }

        return sb.ToString();
    }

    public string EmitTypeScriptDeserializeBody(TypeScriptMember[] members, bool emitSkip)
    {
        var sb = new StringBuilder();

        if (!emitSkip)
        {
            foreach (TypeScriptMember item in members)
            {
                sb.AppendLine($"            value.{item.MemberName} = {item.ReadMethodTemplate};");
            }
        }
        else
        {
            sb.AppendLine("            if (count == 0) return value;");
            for (var i = 0; i < members.Length; i++)
            {
                TypeScriptMember item = members[i];
                sb.AppendLine($"            value.{item.MemberName} = {item.ReadMethodTemplate}; if (count == {i + 1}) return value;");
            }
        }


        return sb.ToString();
    }
}