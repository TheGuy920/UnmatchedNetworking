using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace MemoryPack.Generator;

public class EquatableTypeSymbol(ITypeSymbol typeSymbol) : IEquatable<EquatableTypeSymbol>
{
    // Used for build argument parser, maybe ok to equals name.
    public ITypeSymbol TypeSymbol => typeSymbol;

    public TypeKind TypeKind { get; } = typeSymbol.TypeKind;
    public SpecialType SpecialType { get; } = typeSymbol.SpecialType;

    public bool Equals(EquatableTypeSymbol other)
    {
        if (this.TypeKind != other.TypeKind) return false;
        if (this.SpecialType != other.SpecialType) return false;
        if (this.TypeSymbol.Name != other.TypeSymbol.Name) return false;

        return this.TypeSymbol.EqualsNamespaceAndName(other.TypeSymbol);
    }

    // GetMembers is called for Enum and fields is not condition for command equality.
    public ImmutableArray<ISymbol> GetMembers() => typeSymbol.GetMembers();

    public string ToFullyQualifiedFormatDisplayString() => typeSymbol.ToFullyQualifiedFormatDisplayString();

    public string ToDisplayString(NullableFlowState state, SymbolDisplayFormat format) => typeSymbol.ToDisplayString(state, format);
}

internal static class EquatableTypeSymbolExtensions
{
    public static EquatableTypeSymbol ToEquatable(this ITypeSymbol typeSymbol) => new(typeSymbol);
}