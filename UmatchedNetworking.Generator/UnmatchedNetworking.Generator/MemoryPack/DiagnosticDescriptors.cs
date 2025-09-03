using Microsoft.CodeAnalysis;

namespace MemoryPack.Generator;

internal static class DiagnosticDescriptors
{
    private const string Category = "GenerateMemoryPack";

    public static readonly DiagnosticDescriptor MustBePartial = new(
        "MEMPACK001",
        "MemoryPackable object must be partial",
        "The MemoryPackable object '{0}' must be partial",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor AbstractMustUnion = new(
        "MEMPACK003",
        "abstract/interface type of MemoryPackable object must annotate with Union",
        "abstract/interface type of MemoryPackable object '{0}' must annotate with Union",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor MultipleCtorWithoutAttribute = new(
        "MEMPACK004",
        "Require [MemoryPackConstructor] when exists multiple constructors",
        "The MemoryPackable object '{0}' must annotate with [MemoryPackConstructor] when exists multiple constructors",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor MultipleCtorAttribute = new(
        "MEMPACK005",
        "[MemoryPackConstructor] exists in multiple constructors",
        "Mupltiple [MemoryPackConstructor] exists in '{0}' but allows only single ctor",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor ConstructorHasNoMatchedParameter = new(
        "MEMPACK006",
        "MemoryPackObject's constructor has no matched parameter",
        "The MemoryPackable object '{0}' constructor's parameter '{1}' must match a serialized member name(case-insensitive)",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor OnMethodHasParameter = new(
        "MEMPACK007",
        "MemoryPackObject's On*** methods must has no parameter",
        "The MemoryPackable object '{0}''s '{1}' method must has no parameter",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor OnMethodInUnamannagedType = new(
        "MEMPACK008",
        "MemoryPackObject's On*** methods can't annotate in unamnaged struct",
        "The MemoryPackable object '{0}' is unmanaged struct that can't annotate On***Attribute however '{1}' method annotaed",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor OverrideMemberCantAddAnnotation = new(
        "MEMPACK009",
        "Override member can't annotate Ignore/Include attribute",
        "The MemoryPackable object '{0}' override member '{1}' can't annotate {2} attribute",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor SealedTypeCantBeUnion = new(
        "MEMPACK010",
        "Sealed type can't be union",
        "The MemoryPackable object '{0}' is sealed type so can't be Union",
        Category,
        DiagnosticSeverity.Error,
        true);


    public static readonly DiagnosticDescriptor ConcreteTypeCantBeUnion = new(
        "MEMPACK011",
        "Concrete type can't be union",
        "The MemoryPackable object '{0}' can be Union, only allow abstract or interface",
        Category,
        DiagnosticSeverity.Error,
        true);


    public static readonly DiagnosticDescriptor UnionTagDuplicate = new(
        "MEMPACK012",
        "Union tag is duplicate",
        "The MemoryPackable object '{0}' union tag value is duplicate",
        Category,
        DiagnosticSeverity.Error,
        true);


    public static readonly DiagnosticDescriptor UnionMemberTypeNotImplementBaseType = new(
        "MEMPACK013",
        "Union member not implement union interface",
        "The MemoryPackable object '{0}' union member '{1}' not implement union interface",
        Category,
        DiagnosticSeverity.Error,
        true);


    public static readonly DiagnosticDescriptor UnionMemberTypeNotDerivedBaseType = new(
        "MEMPACK014",
        "Union member not dervided union base type",
        "The MemoryPackable object '{0}' union member '{1}' not derived union type",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor UnionMemberNotAllowStruct = new(
        "MEMPACK015",
        "Union member can't be struct",
        "The MemoryPackable object '{0}' union member '{1}' can't be member, not allows struct",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor UnionMemberMustBeMemoryPackable = new(
        "MEMPACK016",
        "Union member must be MemoryPackable",
        "The MemoryPackable object '{0}' union member '{1}' must be MemoryPackable",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor MembersCountOver250 = new(
        "MEMPACK017",
        "Members count limit",
        "The MemoryPackable object '{0}' member count is '{1}', however limit size is 249",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor MemberCantSerializeType = new(
        "MEMPACK018",
        "Member can't serialize type",
        "The MemoryPackable object '{0}' member '{1}' type is '{2}' that can't serialize",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor MemberIsNotMemoryPackable = new(
        "MEMPACK019",
        "Member is not MemoryPackable object",
        "The MemoryPackable object '{0}' member '{1}' type '{2}' is not MemoryPackable. Annotate [MemoryPackable] to '{2}' or if external type that can serialize, annotate `[MemoryPackAllowSerialize]` to member",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor TypeIsRefStruct = new(
        "MEMPACK020",
        "Type is ref struct",
        "The MemoryPackable object '{0}' is ref struct, it can not serialize",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor MemberIsRefStruct = new(
        "MEMPACK021",
        "Member is ref struct",
        "The MemoryPackable object '{0}' member '{1}' type '{2}' is ref struct, it can not serialize",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor CollectionGenerateIsAbstract = new(
        "MEMPACK022",
        "Collection type not allows interface/abstract",
        "The MemoryPackable object '{0}' is GenerateType.Collection but interface/abstract, only allows concrete type",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor CollectionGenerateNotImplementedInterface = new(
        "MEMPACK023",
        "Collection type must implement collection interface",
        "The MemoryPackable object '{0}' is GenerateType.Collection but not implemented collection interface(ICollection<T>/ISet<T>/IDictionary<TKey,TValue>)",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor CollectionGenerateNoParameterlessConstructor = new(
        "MEMPACK024",
        "Collection type must require parameterless constructor",
        "The MemoryPackable object '{0}' is GenerateType.Collection but not exists parameterless constructor",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor AllMembersMustAnnotateOrder = new(
        "MEMPACK025",
        "All members must annotate MemoryPackOrder when SerializeLayout.Explicit",
        "The MemoryPackable object '{0}' member '{1}' is not annotated MemoryPackOrder",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor AllMembersMustBeContinuousNumber = new(
        "MEMPACK026",
        "All MemoryPackOrder members must be continuous number from zero",
        "The MemoryPackable object '{0}' member '{1}' is not continuous number from zero",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor GenerateTypeScriptMustBeMemoryPackable = new(
        "MEMPACK027",
        "GenerateTypeScript must be MemoryPackable",
        "Type '{0}' is annotated GenerateTypeScript but not annotated MemoryPackable",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor GenerateTypeScriptOnlyAllowsGenerateTypeObject = new(
        "MEMPACK028",
        "GenerateTypeScript must be MemoryPackable(GenerateType.Object)",
        "Type '{0}' is annotated GenerateTypeScript, its MemoryPackable only allows GenerateType.Object",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor GenerateTypeScriptDoesNotAllowGenerics = new(
        "MEMPACK029",
        "GenerateTypeScript type does not allow generics",
        "Type '{0}' is annotated GenerateTypeScript that does not allow generics parameter",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor GenerateTypeScriptDoesNotAllowLongEnum = new(
        "MEMPACK030",
        "GenerateTypeScript type does not allow 64bit enum",
        "GenerateTypeScript type '{0}' has not support 64bit(long/ulong) enum type '{1}', 64bit enum is not supported in typescript generation",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor GenerateTypeScriptNotSupportedType = new(
        "MEMPACK031",
        "not allow GenerateTypeScript type",
        "GenerateTypeScript type '{0}' member '{1}' type '{2}' is not supported type in typescript generation",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor GenerateTypeScriptNotSupportedCustomFormatter = new(
        "MEMPACK032",
        "not allow GenerateTypeScript type",
        "GenerateTypeScript type '{0}' member '{1}' is annnotated [MemoryPackCustomFormatter] that not supported in typescript generation",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor CircularReferenceOnlyAllowsParameterlessConstructor = new(
        "MEMPACK033",
        "CircularReference MemoryPack Object must require parameterless constructor",
        "The MemoryPackable object '{0}' is GenerateType.CircularReference but not exists parameterless constructor.",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor UnamangedStructWithLayoutAutoField = new(
        "MEMPACK034",
        "Before .NET 7 unmanaged struct must annotate LayoutKind.Auto or Explicit",
        "The unmanaged struct '{0}' has LayoutKind.Auto field('{1}'). Before .NET 7, if field contains Auto then automatically promote to LayoutKind.Auto but .NET 7 is Sequential so breaking binary compatibility when runtime upgraded. To safety, you have to annotate [StructLayout(LayoutKind.Auto)] or LayoutKind.Explicit to type.",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor UnamangedStructMemoryPackCtor = new(
        "MEMPACK035",
        "Unamanged strcut does not allow [MemoryPackConstructor]",
        "The unamanged struct '{0}' can not annotate with [MemoryPackConstructor] because don't call any constructors",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor InheritTypeCanNotIncludeParentPrivateMember = new(
        "MEMPACK036",
        "Inherit type can not include private member",
        "Type '{0}' can not include parent type's private member '{1}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor ReadOnlyFieldMustBeConstructorMember = new(
        "MEMPACK037",
        "Readonly field must be constructor member",
        "Type '{0}' readonly field '{1}' must be constructor member",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor DuplicateOrderDoesNotAllow = new(
        "MEMPACK038",
        "All members order must be unique",
        "The MemoryPackable object '{0}' member '{1}' is duplicated order between '{2}'.",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor GenerateTypeCannotSpeciyToUnionBaseType = new(
        "MEMPACK039",
        "GenerateType cannot be specified for the Union base type itself",
        "The MemoryPackable object '{0}' cannot specify '{1}'. Because it is Union base type.",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor SuppressDefaultInitializationMustBeSettable = new(
        "MEMPACK040",
        "Readonly member cannot specify [SuppressDefaultInitialization]",
        "The MemoryPackable object '{0}' member '{1}' has [SuppressDefaultInitialization], it cannot be readonly, init-only and required.",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor VersionTolerantOnUnmanagedStruct = new(
        "MEMPACK041",
        "Invalid usage of VersionTolerant on unmanaged struct",
        "The unmanaged struct '{0}' cannot be used for VersionTolerant serialization.",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor NestedContainingTypesMustBePartial = new(
        "MEMPACK042",
        "Nested MemoryPackable object's containing type(s) must be partial",
        "The MemoryPackable object '{0}' containing type(s) must be partial",
        Category,
        DiagnosticSeverity.Error,
        true);
}