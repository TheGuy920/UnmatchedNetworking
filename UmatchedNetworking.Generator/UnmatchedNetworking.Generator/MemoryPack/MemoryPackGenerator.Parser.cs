using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemoryPack.Generator;

public enum CollectionKind
{
    None,
    Collection,
    Set,
    Dictionary
}

public enum MemberKind
{
    MemoryPackable, // IMemoryPackable<> or [MemoryPackable]
    Unmanaged,
    Nullable,
    UnmanagedNullable,
    KnownType,
    String,
    Array,
    UnmanagedArray,
    MemoryPackableArray, // T[] where T: IMemoryPackable<T>
    MemoryPackableList, // List<T> where T: IMemoryPackable<T>
    MemoryPackableCollection, // GenerateType.Collection
    MemoryPackableNoGenerate, // GenerateType.NoGenerate
    MemoryPackableUnion,
    Enum,

    // from attribute
    AllowSerialize,
    MemoryPackUnion,

    Object, // others allow
    RefLike, // not allowed
    NonSerializable, // not allowed
    Blank, // blank marker
    CustomFormatter // used [MemoryPackCustomFormatterAttribtue]
}

public partial class TypeMeta
{
    private readonly ReferenceSymbols reference;
    private DiagnosticDescriptor? ctorInvalid;

    public TypeMeta(INamedTypeSymbol symbol, ReferenceSymbols reference)
    {
        this.reference = reference;
        this.Symbol = symbol;

        symbol.TryGetMemoryPackableType(reference, out GenerateType generateType, out SerializeLayout serializeLayout);
        this.GenerateType = generateType;
        this.SerializeLayout = serializeLayout;

        this.TypeName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        this.Constructor = this.ChooseConstructor(symbol, reference);

        this.Members = symbol.GetAllMembers() // iterate includes parent type
                             .Where(x => x is (IFieldSymbol or IPropertySymbol) and { IsStatic: false, IsImplicitlyDeclared: false, CanBeReferencedByName: true })
                             .Reverse()
                             .DistinctBy(x => x.Name) // remove duplicate name(new)
                             .Reverse()
                             .Where(x =>
                             {
                                 bool include = x.ContainsAttribute(reference.MemoryPackIncludeAttribute);
                                 bool ignore = x.ContainsAttribute(reference.MemoryPackIgnoreAttribute);
                                 if (ignore) return false;
                                 if (include) return true;
                                 return x.DeclaredAccessibility is Accessibility.Public;
                             })
                             .Where(x =>
                             {
                                 if (x is IPropertySymbol p)
                                 {
                                     // set only can't be serializable member
                                     if (p.GetMethod == null && p.SetMethod != null)
                                     {
                                         return false;
                                     }

                                     if (p.IsIndexer) return false;
                                 }

                                 return true;
                             })
                             .Select((x, i) => new MemberMeta(x, this.Constructor, reference, i))
                             .OrderBy(x => x.Order)
                             .ToArray();

        this.IsValueType = symbol.IsValueType;
        this.IsUnmanagedType = symbol.IsUnmanagedType;
        this.IsInterfaceOrAbstract = symbol.IsAbstract;
        this.IsUnion = symbol.ContainsAttribute(reference.MemoryPackUnionAttribute);
        this.IsRecord = symbol.IsRecord;
        this.OnSerializing = this.CollectMethod(reference.MemoryPackOnSerializingAttribute, this.IsValueType, false);
        this.OnSerialized = this.CollectMethod(reference.MemoryPackOnSerializedAttribute, this.IsValueType, false);
        this.OnDeserializing = this.CollectMethod(reference.MemoryPackOnDeserializingAttribute, this.IsValueType, true);
        this.OnDeserialized = this.CollectMethod(reference.MemoryPackOnDeserializedAttribute, this.IsValueType, true);

        if (this.IsUnion)
        {
            this.UnionTags = symbol.GetAttributes()
                                   .Where(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, reference.MemoryPackUnionAttribute))
                                   .Where(x => x.ConstructorArguments.Length == 2)
                                   .Select(x => ((ushort)x.ConstructorArguments[0].Value!, (INamedTypeSymbol)x.ConstructorArguments[1].Value!))
                                   .ToArray();
        }
        else
        {
            this.UnionTags = Array.Empty<(ushort, INamedTypeSymbol)>();
        }
    }
    public INamedTypeSymbol Symbol { get; set; }
    public GenerateType GenerateType { get; }
    public SerializeLayout SerializeLayout { get; }

    /// <summary>MinimallyQualifiedFormat(include generics T)</summary>
    public string TypeName { get; }

    public MemberMeta[] Members { get; private set; }
    public bool IsValueType { get; set; }
    public bool IsUnmanagedType { get; }
    public bool IsUnion { get; }
    public bool IsRecord { get; }
    public bool IsInterfaceOrAbstract { get; }
    public IMethodSymbol? Constructor { get; }
    public MethodMeta[] OnSerializing { get; }
    public MethodMeta[] OnSerialized { get; }
    public MethodMeta[] OnDeserializing { get; }
    public MethodMeta[] OnDeserialized { get; }
    public (ushort Tag, INamedTypeSymbol Type)[] UnionTags { get; }
    public bool IsUseEmptyConstructor => this.Constructor == null || this.Constructor.Parameters.IsEmpty;

    // MemoryPack choose class/struct as same rule.
    // If has no explicit constrtucotr, use parameterless one(includes private).
    // If has a one parameterless/parameterized constructor, choose it.
    // If has multiple construcotrs, should apply [MemoryPackConstructor] attribute(no automatically choose one), otherwise generator error it.
    private IMethodSymbol? ChooseConstructor(INamedTypeSymbol symbol, ReferenceSymbols reference)
    {
        IMethodSymbol[] ctors = symbol.InstanceConstructors
                                      .Where(x => !x.IsImplicitlyDeclared) // remove empty ctor(struct always generate it), record's clone ctor
                                      .ToArray();

        if (ctors.Length == 0)
        {
            return null; // allows null as ok(not exists explicitly declared constructor == has implictly empty ctor)
        }

        if (!this.Symbol.IsUnmanagedType && ctors.Length == 1)
        {
            return ctors[0];
        }

        IMethodSymbol[] ctorWithAttrs = ctors.Where(x => x.ContainsAttribute(reference.MemoryPackConstructorAttribute)).ToArray();

        if (this.Symbol.IsUnmanagedType)
        {
            if (ctorWithAttrs.Length != 0)
            {
                this.ctorInvalid = DiagnosticDescriptors.UnamangedStructMemoryPackCtor;
            }

            return null;
        }

        if (ctorWithAttrs.Length == 0)
        {
            this.ctorInvalid = DiagnosticDescriptors.MultipleCtorWithoutAttribute;
            return null;
        }

        if (ctorWithAttrs.Length == 1)
        {
            return ctorWithAttrs[0]; // ok
        }

        this.ctorInvalid = DiagnosticDescriptors.MultipleCtorAttribute;
        return null;
    }

    private MethodMeta[] CollectMethod(INamedTypeSymbol attribute, bool isValueType, bool isReader)
    {
        return this.Symbol.GetMembers()
                   .OfType<IMethodSymbol>()
                   .Where(x => x.ContainsAttribute(attribute))
                   .Select(x => new MethodMeta(x, isValueType, isReader))
                   .ToArray();
    }

    public static (CollectionKind, INamedTypeSymbol?) ParseCollectionKind(INamedTypeSymbol? symbol, ReferenceSymbols reference)
    {
        if (symbol == null) goto NONE;

        INamedTypeSymbol? dictionary = default;
        INamedTypeSymbol? set = default;
        INamedTypeSymbol? collection = default;
        foreach (INamedTypeSymbol? item in symbol.AllInterfaces)
        {
            if (item.EqualsUnconstructedGenericType(reference.KnownTypes.System_Collections_Generic_IDictionary_T))
            {
                dictionary = item;
            }
            else if (item.EqualsUnconstructedGenericType(reference.KnownTypes.System_Collections_Generic_ISet_T))
            {
                set = item;
            }
            else if (item.EqualsUnconstructedGenericType(reference.KnownTypes.System_Collections_Generic_ICollection_T))
            {
                collection = item;
            }
        }

        if (dictionary != null)
        {
            return (CollectionKind.Dictionary, dictionary);
        }

        if (set != null)
        {
            return (CollectionKind.Set, set);
        }

        if (collection != null)
        {
            return (CollectionKind.Collection, collection);
        }

        NONE:
        return (CollectionKind.None, null);
    }

    public bool Validate(TypeDeclarationSyntax syntax, IGeneratorContext context, bool unionFormatter)
    {
        var noError = true;
        if (unionFormatter) goto UNION_VALIDATIONS;

        if (this.GenerateType == GenerateType.NoGenerate) return true;
        if (this.GenerateType is GenerateType.Collection)
        {
            if (this.Symbol.IsAbstract)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.CollectionGenerateIsAbstract, syntax.Identifier.GetLocation(), this.Symbol.Name));
                return false;
            }

            (CollectionKind kind, INamedTypeSymbol? symbol) = ParseCollectionKind(this.Symbol, this.reference);
            if (kind == CollectionKind.None)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.CollectionGenerateNotImplementedInterface, syntax.Identifier.GetLocation(), this.Symbol.Name));
                return false;
            }

            bool hasParameterlessConstructor = this.Symbol.InstanceConstructors
                                                   .Where(x => x.DeclaredAccessibility == Accessibility.Public)
                                                   .Any(x => x.Parameters.Length == 0);
            if (!hasParameterlessConstructor)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.CollectionGenerateNoParameterlessConstructor, syntax.Identifier.GetLocation(), this.Symbol.Name));
                return false;
            }

            return true;
        }

        if (this.GenerateType is GenerateType.CircularReference)
        {
            if (!this.IsUseEmptyConstructor)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.CircularReferenceOnlyAllowsParameterlessConstructor, syntax.Identifier.GetLocation(), this.Symbol.Name));
                return false;
            }
        }

        // GenerateType.Objector VersionTorelant validation

        // ref strcut
        if (this.Symbol.IsRefLikeType)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.TypeIsRefStruct, syntax.Identifier.GetLocation(), this.Symbol.Name));
            return false;
        }

        // interface/abstract but not union
        if (this.IsInterfaceOrAbstract && !this.IsUnion)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.AbstractMustUnion, syntax.Identifier.GetLocation(), this.Symbol.Name));
            noError = false;
        }

        // ctor
        if (this.ctorInvalid != null)
        {
            context.ReportDiagnostic(Diagnostic.Create(this.ctorInvalid, syntax.Identifier.GetLocation(), this.Symbol.Name));
            noError = false;
        }

        if (this.IsUnmanagedType)
        {
            if (this.GenerateType is GenerateType.VersionTolerant)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.VersionTolerantOnUnmanagedStruct, syntax.Identifier.GetLocation(), this.Symbol.Name));
                noError = false;
            }

            Tuple<ITypeSymbol, LayoutKind>?[] structLayoutFields = this.Symbol.GetAllMembers()
                                                                       .OfType<IFieldSymbol>()
                                                                       .Select(x =>
                                                                       {
                                                                           if (x.IsStatic) return null;

                                                                           // ValueTuple, DateTime, DateTimeOffset is auto but can not get from Roslyn GetAttributes.

                                                                           if (SymbolEqualityComparer.Default.Equals(x.Type, this.reference.KnownTypes.System_DateTime) ||
                                                                               SymbolEqualityComparer.Default.Equals(x.Type, this.reference.KnownTypes.System_DateTimeOffset))
                                                                           {
                                                                               return Tuple.Create(x.Type, LayoutKind.Auto);
                                                                           }

                                                                           if (x.Type is INamedTypeSymbol nts && nts.IsGenericType)
                                                                           {
                                                                               string fullyQualifiedString = nts.ConstructUnboundGenericType().FullyQualifiedToString();
                                                                               if (fullyQualifiedString.StartsWith("global::System.ValueTuple<"))
                                                                               {
                                                                                   return Tuple.Create(x.Type, LayoutKind.Auto);
                                                                               }
                                                                           }

                                                                           AttributeData? structLayout = x.Type.GetAttribute(this.reference.KnownTypes.System_Runtime_InteropServices_StructLayout);
                                                                           object? layoutKind = structLayout != null && structLayout.ConstructorArguments.Length != 0
                                                                               ? structLayout.ConstructorArguments[0].Value
                                                                               : null;

                                                                           if (layoutKind != null)
                                                                           {
                                                                               return Tuple.Create(x.Type, (LayoutKind)layoutKind);
                                                                           }

                                                                           return null;
                                                                       })
                                                                       .Where(x => x != null && x.Item2 == LayoutKind.Auto)
                                                                       .ToArray();

            // has auto field, should mark Auto in lower Net6
            if (structLayoutFields.Length != 0)
            {
                AttributeData? structLayout = this.Symbol.GetAttribute(this.reference.KnownTypes.System_Runtime_InteropServices_StructLayout);
                object? layoutKind = structLayout != null && structLayout.ConstructorArguments.Length != 0
                    ? structLayout.ConstructorArguments[0].Value
                    : null;

                if (layoutKind == null || (LayoutKind)layoutKind == LayoutKind.Sequential)
                {
                    string autoTypes = string.Join(", ", structLayoutFields.Select(x => x!.Item1.Name));

                    if (!context.IsNet7OrGreater)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UnamangedStructWithLayoutAutoField, syntax.Identifier.GetLocation(), this.Symbol.Name, autoTypes));
                        noError = false;
                    }
                }
            }
        }
        else
        {
            // check ctor members
            if (this.Constructor != null)
            {
                foreach (IParameterSymbol? parameter in this.Constructor.Parameters)
                {
                    if (!this.Members.ContainsConstructorParameter(parameter))
                    {
                        Location location = this.Constructor.Locations.FirstOrDefault() ?? syntax.Identifier.GetLocation();

                        context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ConstructorHasNoMatchedParameter, location, this.Symbol.Name, parameter.Name));
                        noError = false;
                    }
                }
            }

            foreach (MemberMeta item in this.Members)
            {
                if (item.IsField && ((IFieldSymbol)item.Symbol).IsReadOnly && !item.IsConstructorParameter)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ReadOnlyFieldMustBeConstructorMember, item.GetLocation(syntax), this.Symbol.Name, item.Name));
                    noError = false;
                }
                else if (item is { SuppressDefaultInitialization: true, IsAssignable: false })
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SuppressDefaultInitializationMustBeSettable, item.GetLocation(syntax), this.Symbol.Name, item.Name));
                    noError = false;
                }
            }
        }

        // methods
        foreach (MethodMeta? item in this.OnSerializing.Concat(this.OnSerialized).Concat(this.OnDeserializing).Concat(this.OnDeserialized))
        {
            // diagnostics location should be method identifier
            // however methodsymbol -> methodsyntax is slightly hard so use type identifier instead.

            if (this.IsUnmanagedType)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.OnMethodInUnamannagedType, item.GetLocation(syntax), this.Symbol.Name, item.Name));
                noError = false;
                continue;
            }

            if (item.Symbol.Parameters.Length != 0)
            {
                // if (ref reader/writer), ok.
                if (item.Symbol.Parameters.Length == 2)
                {
                    if (item.Symbol.Parameters[0].RefKind == RefKind.Ref && item.Symbol.Parameters[1].RefKind == RefKind.Ref)
                    {
                        // ref ref is ok.
                        continue;
                    }
                }

                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.OnMethodHasParameter, item.GetLocation(syntax), this.Symbol.Name, item.Name));
                noError = false;
            }
        }

        if (this.Symbol.BaseType != null)
        {
            // Member override member can't annotate[Ignore][Include]
            foreach (ISymbol? item in this.Symbol.GetAllMembers(false))
            {
                if (item.IsOverride)
                {
                    bool include = item.ContainsAttribute(this.reference.MemoryPackIncludeAttribute);
                    bool ignore = item.ContainsAttribute(this.reference.MemoryPackIgnoreAttribute);
                    if (include || ignore)
                    {
                        Location location = item.Locations.FirstOrDefault() ?? syntax.Identifier.GetLocation();

                        string attr = include ? "MemoryPackInclude" : "MemoryPackIgnore";
                        context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.OverrideMemberCantAddAnnotation, location, this.Symbol.Name, item.Name, attr));
                        noError = false;
                    }
                }
            }

            // inherit type can not serialize parent private member
            foreach (ISymbol? item in this.Symbol.GetParentMembers())
            {
                bool include = item.ContainsAttribute(this.reference.MemoryPackIncludeAttribute);
                bool ignore = item.ContainsAttribute(this.reference.MemoryPackIgnoreAttribute);
                if (include && item.DeclaredAccessibility == Accessibility.Private)
                {
                    Location location = item.Locations.FirstOrDefault() ?? syntax.Identifier.GetLocation();
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InheritTypeCanNotIncludeParentPrivateMember, location, this.Symbol.Name, item.Name));
                    noError = false;
                }
            }
        }

        // ALl Members
        if (this.Members.Length >= 250) // MemoryPackCode.Reserved1
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MembersCountOver250, syntax.Identifier.GetLocation(), this.Symbol.Name, this.Members.Length));
            noError = false;
        }

        // exists can't serialize member
        foreach (MemberMeta item in this.Members)
        {
            if (item.Kind == MemberKind.NonSerializable)
            {
                if (item.MemberType.SpecialType is SpecialType.System_Object or SpecialType.System_Array or SpecialType.System_Delegate or SpecialType.System_MulticastDelegate ||
                    item.MemberType.TypeKind == TypeKind.Delegate)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MemberCantSerializeType, item.GetLocation(syntax), this.Symbol.Name, item.Name,
                        item.MemberType.FullyQualifiedToString()));
                    noError = false;
                }
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MemberIsNotMemoryPackable, item.GetLocation(syntax), this.Symbol.Name, item.Name,
                        item.MemberType.FullyQualifiedToString()));
                    noError = false;
                }
            }
            else if (item.Kind == MemberKind.RefLike)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MemberIsRefStruct, item.GetLocation(syntax), this.Symbol.Name, item.Name, item.MemberType.FullyQualifiedToString()));
                noError = false;
            }
        }

        // order
        if (this.SerializeLayout == SerializeLayout.Explicit)
        {
            // All members must annotate MemoryPackOrder
            foreach (MemberMeta item in this.Members)
            {
                if (!item.HasExplicitOrder)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.AllMembersMustAnnotateOrder, item.GetLocation(syntax), this.Symbol.Name, item.Name));
                    noError = false;
                }
            }

            // don't allow duplicate order
            var orderSet = new Dictionary<int, MemberMeta>(this.Members.Length);
            foreach (MemberMeta item in this.Members)
            {
                if (orderSet.TryGetValue(item.Order, out MemberMeta? duplicateMember))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.DuplicateOrderDoesNotAllow, item.GetLocation(syntax), this.Symbol.Name, item.Name, duplicateMember.Name));
                    noError = false;
                }
                else
                {
                    orderSet.Add(item.Order, item);
                }
            }

            // Annotated MemoryPackOrder must be continuous number from zero if GenerateType.Object.
            if (noError && this.GenerateType == GenerateType.Object)
            {
                var expectedOrder = 0;
                foreach (MemberMeta item in this.Members)
                {
                    if (item.Order != expectedOrder)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.AllMembersMustBeContinuousNumber, item.GetLocation(syntax), this.Symbol.Name, item.Name));
                        noError = false;
                        break;
                    }

                    expectedOrder++;
                }
            }
        }

        // Union validations
        UNION_VALIDATIONS:
        if (this.IsUnion)
        {
            if (this.Symbol.IsSealed)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SealedTypeCantBeUnion, syntax.Identifier.GetLocation(), this.Symbol.Name));
                noError = false;
            }

            if (!this.Symbol.IsAbstract)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ConcreteTypeCantBeUnion, syntax.Identifier.GetLocation(), this.Symbol.Name));
                noError = false;
            }

            if (this.GenerateType != GenerateType.Union)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.GenerateTypeCannotSpeciyToUnionBaseType, syntax.Identifier.GetLocation(), this.Symbol.Name, this.GenerateType));
                noError = false;
            }

            if (this.UnionTags.Select(x => x.Tag).HasDuplicate())
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UnionTagDuplicate, syntax.Identifier.GetLocation(), this.Symbol.Name));
                noError = false;
            }

            foreach ((ushort Tag, INamedTypeSymbol Type) item in this.UnionTags)
            {
                // type does not derived target symbol
                if (this.Symbol.TypeKind == TypeKind.Interface)
                {
                    // interface, check interfaces.
                    bool check = item.Type.IsGenericType
                        ? item.Type.OriginalDefinition.AllInterfaces.Any(x => x.EqualsUnconstructedGenericType(this.Symbol))
                        : item.Type.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, this.Symbol));

                    if (!check)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UnionMemberTypeNotImplementBaseType, syntax.Identifier.GetLocation(), this.Symbol.Name, item.Type.Name));
                        noError = false;
                    }
                }
                else
                {
                    // abstract type, check base.
                    bool check = item.Type.IsGenericType
                        ? item.Type.OriginalDefinition.GetAllBaseTypes().Any(x => x.EqualsUnconstructedGenericType(this.Symbol))
                        : item.Type.GetAllBaseTypes().Any(x => SymbolEqualityComparer.Default.Equals(x, this.Symbol));

                    if (!check)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UnionMemberTypeNotDerivedBaseType, syntax.Identifier.GetLocation(), this.Symbol.Name, item.Type.Name));
                        noError = false;
                    }
                }

                if (item.Type.IsValueType)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UnionMemberNotAllowStruct, syntax.Identifier.GetLocation(), this.Symbol.Name, item.Type.Name));
                    noError = false;
                }

                if (!item.Type.ContainsAttribute(this.reference.MemoryPackableAttribute))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UnionMemberMustBeMemoryPackable, syntax.Identifier.GetLocation(), this.Symbol.Name, item.Type.Name));
                    noError = false;
                }
            }
        }

        return noError;
    }

    public override string ToString()
        => this.TypeName;
}

partial class MemberMeta
{
    private MemberMeta(int order)
    {
        this.Symbol = null!;
        this.Name = null!;
        this.MemberType = null!;
        this.Order = order;
        this.Kind = MemberKind.Blank;
    }
    public ISymbol Symbol { get; }
    public string Name { get; }
    public ITypeSymbol MemberType { get; }
    public INamedTypeSymbol? CustomFormatter { get; }
    public string? CustomFormatterName { get; }
    public bool IsField { get; }
    public bool IsProperty { get; }
    public bool IsSettable { get; }
    public bool IsAssignable { get; }
    public bool IsConstructorParameter { get; }
    public string? ConstructorParameterName { get; }
    public int Order { get; }
    public bool HasExplicitOrder { get; }
    public MemberKind Kind { get; }
    public bool SuppressDefaultInitialization { get; }

    public MemberMeta(ISymbol symbol, IMethodSymbol? constructor, ReferenceSymbols references, int sequentialOrder)
    {
        this.Symbol = symbol;
        this.Name = symbol.Name;
        this.Order = sequentialOrder;
        this.SuppressDefaultInitialization = symbol.ContainsAttribute(references.SkipOverwriteDefaultAttribute);
        AttributeData? orderAttr = symbol.GetAttribute(references.MemoryPackOrderAttribute);
        if (orderAttr != null)
        {
            this.Order = (int)(orderAttr.ConstructorArguments[0].Value ?? sequentialOrder);
            this.HasExplicitOrder = true;
        }
        else
        {
            this.HasExplicitOrder = false;
        }

        if (constructor != null)
        {
            this.IsConstructorParameter = constructor.TryGetConstructorParameter(symbol, out IParameterSymbol? constructorParameter);
            this.ConstructorParameterName = constructorParameter?.Name;
        }
        else
        {
            this.IsConstructorParameter = false;
        }

        if (symbol is IFieldSymbol f)
        {
            this.IsProperty = false;
            this.IsField = true;
            this.IsSettable = !f.IsReadOnly; // readonly field can not set.
            this.IsAssignable = this.IsSettable
                                #if !ROSLYN3
                                && !f.IsRequired
                #endif
                ;
            this.MemberType = f.Type;
        }
        else if (symbol is IPropertySymbol p)
        {
            this.IsProperty = true;
            this.IsField = false;
            this.IsSettable = !p.IsReadOnly;
            this.IsAssignable = this.IsSettable
                                #if !ROSLYN3
                                && !p.IsRequired
                                && p.SetMethod != null && !p.SetMethod.IsInitOnly;
                                #endif
            this.MemberType = p.Type;
        }
        else
        {
            throw new Exception("member is not field or property.");
        }

        if (references.MemoryPackCustomFormatterAttribute != null)
        {
            var genericFormatter = false;
            AttributeData? customFormatterAttr = symbol.GetImplAttribute(references.MemoryPackCustomFormatterAttribute);
            if (customFormatterAttr == null && references.MemoryPackCustomFormatter2Attribute != null)
            {
                customFormatterAttr = symbol.GetImplAttribute(references.MemoryPackCustomFormatter2Attribute);
                genericFormatter = true;
            }

            if (customFormatterAttr != null)
            {
                this.CustomFormatter = customFormatterAttr.AttributeClass!;
                this.Kind = MemberKind.CustomFormatter;

                string formatterName;
                if (genericFormatter)
                {
                    formatterName = this.CustomFormatter.GetAllBaseTypes().First(x => x.EqualsUnconstructedGenericType(references.MemoryPackCustomFormatter2Attribute!))
                                        .TypeArguments[0].FullyQualifiedToString();
                }
                else
                {
                    formatterName = $"IMemoryPackFormatter<{this.MemberType.FullyQualifiedToString()}>";
                }

                this.CustomFormatterName = formatterName;
                return;
            }
        }

        this.Kind = ParseMemberKind(symbol, this.MemberType, references);
    }

    public static MemberMeta CreateEmpty(int order)
        => new(order);

    public Location GetLocation(TypeDeclarationSyntax fallback)
    {
        Location location = this.Symbol.Locations.FirstOrDefault() ?? fallback.Identifier.GetLocation();
        return location;
    }

    private static MemberKind ParseMemberKind(ISymbol? memberSymbol, ITypeSymbol memberType, ReferenceSymbols references)
    {
        if (memberType.SpecialType is SpecialType.System_Object or SpecialType.System_Array or SpecialType.System_Delegate or SpecialType.System_MulticastDelegate || memberType.TypeKind == TypeKind.Delegate)
        {
            return MemberKind.NonSerializable; // object, Array, delegate is not allowed
        }

        if (memberType.TypeKind == TypeKind.Enum)
        {
            return MemberKind.Enum;
        }

        if (memberType.IsUnmanagedType)
        {
            if (memberType is INamedTypeSymbol unmanagedNts)
            {
                if (unmanagedNts.IsRefLikeType)
                {
                    return MemberKind.RefLike;
                }

                if (unmanagedNts.EqualsUnconstructedGenericType(references.KnownTypes.System_Nullable_T))
                {
                    // unamanged nullable<T> can not pass to where T:unmanaged constraint
                    if (unmanagedNts.TypeArguments[0].IsUnmanagedType)
                    {
                        return MemberKind.UnmanagedNullable;
                    }

                    return MemberKind.Nullable;
                }
            }

            return MemberKind.Unmanaged;
        }

        if (memberType.SpecialType == SpecialType.System_String)
        {
            return MemberKind.String;
        }

        if (memberType.AllInterfaces.Any(x => x.EqualsUnconstructedGenericType(references.IMemoryPackable)))
        {
            return MemberKind.MemoryPackable;
        }

        if (memberType.TryGetMemoryPackableType(references, out GenerateType generateType, out SerializeLayout serializeLayout))
        {
            switch (generateType)
            {
                case GenerateType.Object:
                case GenerateType.VersionTolerant:
                case GenerateType.CircularReference:
                    return MemberKind.MemoryPackable;
                case GenerateType.Collection:
                    return MemberKind.MemoryPackableCollection;
                case GenerateType.Union:
                    return MemberKind.MemoryPackableUnion;
                case GenerateType.NoGenerate:
                default:
                    return MemberKind.MemoryPackableNoGenerate;
            }
        }

        if (memberType.IsWillImplementMemoryPackUnion(references))
        {
            return MemberKind.MemoryPackUnion;
        }

        if (memberType.TypeKind == TypeKind.Array)
        {
            if (memberType is IArrayTypeSymbol array)
            {
                if (array.IsSZArray)
                {
                    ITypeSymbol elemType = array.ElementType;
                    if (elemType.IsUnmanagedType)
                    {
                        if (elemType is INamedTypeSymbol unmanagedNts && unmanagedNts.EqualsUnconstructedGenericType(references.KnownTypes.System_Nullable_T))
                        {
                            // T?[] can not use Write/ReadUnmanagedArray
                            return MemberKind.Array;
                        }

                        return MemberKind.UnmanagedArray;
                    }

                    if (elemType.TryGetMemoryPackableType(references, out GenerateType elemGenerateType, out SerializeLayout _) &&
                        elemGenerateType is GenerateType.Object or GenerateType.VersionTolerant or GenerateType.CircularReference)
                    {
                        return MemberKind.MemoryPackableArray;
                    }

                    return MemberKind.Array;
                }

                // allows 2, 3, 4
                if (array.Rank <= 4)
                {
                    return MemberKind.Object;
                }
            }

            return MemberKind.NonSerializable;
        }

        if (memberType.TypeKind == TypeKind.TypeParameter) // T
        {
            return MemberKind.Object;
        }

        // or non unmanaged type
        if (memberType is INamedTypeSymbol nts)
        {
            if (nts.IsRefLikeType)
            {
                return MemberKind.RefLike;
            }

            if (nts.EqualsUnconstructedGenericType(references.KnownTypes.System_Nullable_T))
            {
                return MemberKind.Nullable;
            }

            if (nts.EqualsUnconstructedGenericType(references.KnownTypes.System_Collections_Generic_List_T))
            {
                if (nts.TypeArguments[0].TryGetMemoryPackableType(references, out GenerateType elemGenerateType, out SerializeLayout _) &&
                    elemGenerateType is GenerateType.Object or GenerateType.VersionTolerant or GenerateType.CircularReference)
                {
                    return MemberKind.MemoryPackableList;
                }

                return MemberKind.KnownType;
            }
        }

        if (references.KnownTypes.Contains(memberType))
        {
            return MemberKind.KnownType;
        }

        if (memberSymbol != null)
        {
            if (memberSymbol.ContainsAttribute(references.MemoryPackAllowSerializeAttribute))
            {
                return MemberKind.AllowSerialize;
            }
        }

        return MemberKind.NonSerializable; // maybe can't serialize, diagnostics target
    }
}

public partial class MethodMeta
{
    public MethodMeta(IMethodSymbol symbol, bool isValueType, bool isReader)
    {
        this.Symbol = symbol;
        this.Name = symbol.Name;
        this.IsStatic = symbol.IsStatic;
        this.IsValueType = isValueType;

        bool hasArg = symbol.Parameters.Length != 0;
        if (hasArg)
        {
            if (isReader)
            {
                this.UseReaderArgument = true;
            }
            else
            {
                this.UseWriterArgument = true;
            }
        }
    }
    public IMethodSymbol Symbol { get; }
    public string Name { get; }
    public bool IsStatic { get; }
    public bool IsValueType { get; }
    public bool UseReaderArgument { get; }
    public bool UseWriterArgument { get; }

    public Location GetLocation(TypeDeclarationSyntax fallback)
    {
        Location location = this.Symbol.Locations.FirstOrDefault() ?? fallback.Identifier.GetLocation();
        return location;
    }
}