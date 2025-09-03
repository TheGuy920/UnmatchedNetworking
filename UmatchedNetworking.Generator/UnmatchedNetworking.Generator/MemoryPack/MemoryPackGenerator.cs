using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemoryPack.Generator;

// dotnet/runtime generators.

// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Text.RegularExpressions/gen/
// https://github.com/dotnet/runtime/tree/main/src/libraries/System.Text.Json/gen
// https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/gen
// https://github.com/dotnet/runtime/tree/main/src/libraries/Microsoft.Extensions.Logging.Abstractions/gen
// https://github.com/dotnet/runtime/tree/main/src/libraries/System.Runtime.InteropServices.JavaScript/gen/JSImportGenerator
// https://github.com/dotnet/runtime/tree/main/src/libraries/System.Runtime.InteropServices/gen/LibraryImportGenerator
// https://github.com/dotnet/runtime/tree/main/src/tests/Common/XUnitWrapperGenerator

// documents, blogs.

// https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md
// https://andrewlock.net/creating-a-source-generator-part-1-creating-an-incremental-source-generator/
// https://qiita.com/WiZLite/items/48f37278cf13be899e40
// https://zenn.dev/pcysl5edgo/articles/6d9be0dd99c008
// https://neue.cc/2021/05/08_600.html
// https://www.thinktecture.com/en/net/roslyn-source-generators-introduction/

// for check generated file
// <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
// <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>

[Generator(LanguageNames.CSharp)]
public partial class MemoryPackGenerator : IIncrementalGenerator
{
    public const string MemoryPackableAttributeFullName = "MemoryPack.MemoryPackableAttribute";
    public const string MemoryPackUnionFormatterAttributeFullName = "MemoryPack.MemoryPackUnionFormatterAttribute";
    public const string GenerateTypeScriptAttributeFullName = "MemoryPack.GenerateTypeScriptAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // no need RegisterPostInitializationOutput

        this.RegisterMemoryPackable(context);
        this.RegisterTypeScript(context);
    }

    private void RegisterMemoryPackable(IncrementalGeneratorInitializationContext context)
    {
        // return dir of info output or null .
        IncrementalValueProvider<string?> logProvider = context.AnalyzerConfigOptionsProvider
                                                               .Select((configOptions, token) =>
                                                               {
                                                                   if (configOptions.GlobalOptions.TryGetValue("build_property.MemoryPackGenerator_SerializationInfoOutputDirectory", out string? path))
                                                                   {
                                                                       return path;
                                                                   }

                                                                   return null;
                                                               })
                                                               .WithTrackingName("MemoryPack.MemoryPackable.0_AnalyzerConfigOptionsProvider"); // annotate for IncrementalGeneratorTest

        IncrementalValueProvider<(LanguageVersion langVersion, bool net7)> parseOptions = context.ParseOptionsProvider
                                                                                                 .Select((parseOptions, token) =>
                                                                                                 {
                                                                                                     var csOptions = (CSharpParseOptions)parseOptions;
                                                                                                     LanguageVersion langVersion = csOptions.LanguageVersion;
                                                                                                     bool net7 = csOptions.PreprocessorSymbolNames.Contains("NET7_0_OR_GREATER");
                                                                                                     return (langVersion, net7);
                                                                                                 })
                                                                                                 .WithTrackingName("MemoryPack.MemoryPackable.0_ParseOptionsProvider");

        IncrementalValuesProvider<TypeDeclarationSyntax> typeDeclarations = context.SyntaxProvider.ForAttributeWithMetadataName(
                                                                                       MemoryPackableAttributeFullName,
                                                                                       static (node, token) =>
                                                                                       {
                                                                                           // search [MemoryPackable] class or struct or interface or record
                                                                                           return node is ClassDeclarationSyntax
                                                                                               or StructDeclarationSyntax
                                                                                               or RecordDeclarationSyntax
                                                                                               or InterfaceDeclarationSyntax;
                                                                                       },
                                                                                       static (context, token) => { return (TypeDeclarationSyntax)context.TargetNode; })
                                                                                   .WithTrackingName("MemoryPack.MemoryPackable.1_ForAttributeMemoryPackableAttribute");

        IncrementalValuesProvider<TypeDeclarationSyntax> typeDeclarations2 = context.SyntaxProvider.ForAttributeWithMetadataName(
                                                                                        MemoryPackUnionFormatterAttributeFullName,
                                                                                        static (node, token) => { return node is ClassDeclarationSyntax; },
                                                                                        static (context, token) => { return (TypeDeclarationSyntax)context.TargetNode; })
                                                                                    .WithTrackingName("MemoryPack.MemoryPackable.1_ForAttributeMemoryPackUnion");

        {
            IncrementalValuesProvider<(((TypeDeclarationSyntax, Compilation) Left, string? Right) Left, (LanguageVersion langVersion, bool net7) Right)> source = typeDeclarations
                .Combine(context.CompilationProvider)
                .WithComparer(Comparer.Instance)
                .Combine(logProvider)
                .Combine(parseOptions)
                .WithTrackingName("MemoryPack.MemoryPackable.2_MemoryPackableCombined");

            context.RegisterSourceOutput(source, static (context, source) =>
            {
                (TypeDeclarationSyntax? typeDeclaration, Compilation? compilation) = source.Left.Item1;
                string? logPath = source.Left.Item2;
                (LanguageVersion langVersion, bool net7) = source.Right;

                // Generate(typeDeclaration, compilation, new GeneratorContext(context, langVersion, net7));
            });
        }
        {
            IncrementalValuesProvider<(((TypeDeclarationSyntax, Compilation) Left, string? Right) Left, (LanguageVersion langVersion, bool net7) Right)> source = typeDeclarations2
                .Combine(context.CompilationProvider)
                .WithComparer(Comparer.Instance)
                .Combine(logProvider)
                .Combine(parseOptions)
                .WithTrackingName("MemoryPack.MemoryPackable.2_MemoryPackUnionCombined");

            context.RegisterSourceOutput(source, static (context, source) =>
            {
                (TypeDeclarationSyntax? typeDeclaration, Compilation? compilation) = source.Left.Item1;
                string? logPath = source.Left.Item2;
                (LanguageVersion langVersion, bool net7) = source.Right;

                // Generate(typeDeclaration, compilation, new GeneratorContext(context, langVersion, net7));
            });
        }
    }

    private void RegisterTypeScript(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<TypeScriptGenerateOptions?> typeScriptEnabled = context.AnalyzerConfigOptionsProvider
                                                                                        .Select((configOptions, token) =>
                                                                                        {
                                                                                            // https://github.com/dotnet/project-system/blob/main/docs/design-time-builds.md
                                                                                            bool isDesignTimeBuild =
                                                                                                configOptions.GlobalOptions.TryGetValue("build_property.DesignTimeBuild", out string? designTimeBuild) &&
                                                                                                designTimeBuild == "true";

                                                                                            string? path;
                                                                                            if (!configOptions.GlobalOptions.TryGetValue("build_property.MemoryPackGenerator_TypeScriptOutputDirectory",
                                                                                                    out path))
                                                                                            {
                                                                                                path = null;
                                                                                            }

                                                                                            string ext;
                                                                                            if (!configOptions.GlobalOptions.TryGetValue("build_property.MemoryPackGenerator_TypeScriptImportExtension",
                                                                                                    out ext!))
                                                                                            {
                                                                                                ext = ".js";
                                                                                            }

                                                                                            string convertProp;
                                                                                            if (!configOptions.GlobalOptions.TryGetValue("build_property.MemoryPackGenerator_TypeScriptConvertPropertyName",
                                                                                                    out convertProp!))
                                                                                            {
                                                                                                convertProp = "true";
                                                                                            }

                                                                                            if (!configOptions.GlobalOptions.TryGetValue("build_property.MemoryPackGenerator_TypeScriptEnableNullableTypes",
                                                                                                    out string? enableNullableTypes))
                                                                                            {
                                                                                                enableNullableTypes = "false";
                                                                                            }

                                                                                            if (!bool.TryParse(convertProp, out bool convert)) convert = true;

                                                                                            if (path == null) return null;

                                                                                            return new TypeScriptGenerateOptions
                                                                                            {
                                                                                                OutputDirectory = path,
                                                                                                ImportExtension = ext,
                                                                                                ConvertPropertyName = convert,
                                                                                                EnableNullableTypes = bool.TryParse(enableNullableTypes, out bool enabledNullableTypesParsed) &&
                                                                                                                      enabledNullableTypesParsed,
                                                                                                IsDesignTimeBuild = isDesignTimeBuild
                                                                                            };
                                                                                        });

        IncrementalValuesProvider<TypeDeclarationSyntax> typeScriptDeclarations = context.SyntaxProvider.ForAttributeWithMetadataName(
            GenerateTypeScriptAttributeFullName,
            static (node, token) =>
            {
                return node is ClassDeclarationSyntax
                    or RecordDeclarationSyntax
                    or InterfaceDeclarationSyntax;
            },
            static (context, token) => { return (TypeDeclarationSyntax)context.TargetNode; });

        IncrementalValueProvider<ImmutableArray<((TypeDeclarationSyntax, Compilation) Left, TypeScriptGenerateOptions? Right)>> typeScriptGenerateSource = typeScriptDeclarations
            .Combine(context.CompilationProvider)
            .WithComparer(Comparer.Instance)
            .Combine(typeScriptEnabled)
            .Where(x => x.Right != null) // filter
            .Collect();

        context.RegisterSourceOutput(typeScriptGenerateSource, static (context, source) =>
        {
            ReferenceSymbols? reference = null;
            string? generatePath = null;

            var unionMap = new Dictionary<ITypeSymbol, ITypeSymbol>(SymbolEqualityComparer.Default); // <impl, base>
            foreach (((TypeDeclarationSyntax, Compilation) Left, TypeScriptGenerateOptions? Right) item in source)
            {
                TypeScriptGenerateOptions? tsOptions = item.Right;
                if (tsOptions == null) continue;
                if (tsOptions.IsDesignTimeBuild) continue; // designtime build(in IDE), do nothing.

                TypeDeclarationSyntax? syntax = item.Left.Item1;
                Compilation? compilation = item.Left.Item2;
                SemanticModel semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
                var typeSymbol = semanticModel.GetDeclaredSymbol(syntax, context.CancellationToken) as ITypeSymbol;
                if (typeSymbol == null) continue;
                if (reference == null)
                {
                    reference = new ReferenceSymbols(compilation);
                }

                if (generatePath is null && item.Right is { } options)
                {
                    generatePath = options.OutputDirectory;
                }

                bool isUnion = typeSymbol.ContainsAttribute(reference.MemoryPackUnionAttribute);

                if (isUnion)
                {
                    IEnumerable<INamedTypeSymbol> unionTags = typeSymbol.GetAttributes()
                                                                        .Where(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, reference.MemoryPackUnionAttribute))
                                                                        .Where(x => x.ConstructorArguments.Length == 2)
                                                                        .Select(x => (INamedTypeSymbol)x.ConstructorArguments[1].Value!);
                    foreach (INamedTypeSymbol? implType in unionTags)
                    {
                        unionMap[implType] = typeSymbol;
                    }
                }
            }

            if (generatePath != null)
            {
                var collector = new TypeCollector();
                foreach (((TypeDeclarationSyntax, Compilation) Left, TypeScriptGenerateOptions? Right) item in source)
                {
                    TypeDeclarationSyntax? typeDeclaration = item.Left.Item1;
                    Compilation? compilation = item.Left.Item2;

                    if (reference == null)
                    {
                        reference = new ReferenceSymbols(compilation);
                    }

                    TypeMeta? meta = GenerateTypeScript(typeDeclaration, compilation, item.Right!, context, reference, unionMap);
                    if (meta != null)
                    {
                        collector.Visit(meta, false);
                    }
                }

                GenerateEnums(collector.GetEnums(), generatePath);

                // generate runtime
                (string, string)[] runtime = new[]
                {
                    ("MemoryPackWriter.ts", TypeScriptRuntime.MemoryPackWriter),
                    ("MemoryPackReader.ts", TypeScriptRuntime.MemoryPackReader)
                };

                foreach ((string, string) item in runtime)
                {
                    string filePath = Path.Combine(generatePath, item.Item1);
                    // if (!File.Exists(filePath))
                    // {
                    //     File.WriteAllText(filePath, item.Item2, new UTF8Encoding(false));
                    // }
                }
            }
        });
    }

    public class Comparer : IEqualityComparer<(TypeDeclarationSyntax, Compilation)>
    {
        public static readonly Comparer Instance = new();

        public bool Equals((TypeDeclarationSyntax, Compilation) x, (TypeDeclarationSyntax, Compilation) y)
            => x.Item1.Equals(y.Item1);

        public int GetHashCode((TypeDeclarationSyntax, Compilation) obj)
            => obj.Item1.GetHashCode();
    }

    public class GeneratorContext : IGeneratorContext
    {
        private readonly SourceProductionContext context;

        public GeneratorContext(SourceProductionContext context, LanguageVersion languageVersion, bool isNet70OrGreater)
        {
            this.context = context;
            this.LanguageVersion = languageVersion;
            this.IsNet7OrGreater = isNet70OrGreater;
        }

        public CancellationToken CancellationToken => this.context.CancellationToken;

        public LanguageVersion LanguageVersion { get; }

        public bool IsNet7OrGreater { get; }

        public bool IsForUnity => false;

        public void AddSource(string hintName, string source)
        {
            this.context.AddSource(hintName, source);
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            this.context.ReportDiagnostic(diagnostic);
        }
    }
}