using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MemoryPack.Generator;

public class TypeCollector
{
    private readonly HashSet<ITypeSymbol> types = new(SymbolEqualityComparer.Default);

    public void Visit(TypeMeta typeMeta, bool visitInterface)
    {
        this.Visit(typeMeta.Symbol, visitInterface);
        foreach (MemberMeta? item in typeMeta.Members.Where(x => x.Symbol != null))
        {
            this.Visit(item.MemberType, visitInterface);
        }
    }

    public void Visit(ISymbol symbol, bool visitInterface)
    {
        if (symbol is ITypeSymbol typeSymbol)
        {
            // 7~20 is primitive
            if ((int)typeSymbol.SpecialType is >= 7 and <= 20)
            {
                return;
            }

            if (!this.types.Add(typeSymbol))
            {
                return;
            }

            if (typeSymbol is IArrayTypeSymbol array)
            {
                this.Visit(array.ElementType, visitInterface);
            }
            else if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                if (visitInterface)
                {
                    foreach (INamedTypeSymbol? item in namedTypeSymbol.AllInterfaces)
                    {
                        this.Visit(item, visitInterface);
                    }

                    foreach (INamedTypeSymbol? item in namedTypeSymbol.GetAllBaseTypes())
                    {
                        this.Visit(item, visitInterface);
                    }
                }

                if (namedTypeSymbol.IsGenericType)
                {
                    foreach (ITypeSymbol? item in namedTypeSymbol.TypeArguments)
                    {
                        this.Visit(item, visitInterface);
                    }
                }
            }
        }
    }

    public IEnumerable<ITypeSymbol> GetEnums()
    {
        foreach (ITypeSymbol? typeSymbol in this.types)
        {
            if (typeSymbol.TypeKind == TypeKind.Enum)
            {
                yield return typeSymbol;
            }
        }
    }

    public IEnumerable<ITypeSymbol> GetMemoryPackableTypes(ReferenceSymbols reference)
    {
        foreach (ITypeSymbol? typeSymbol in this.types)
        {
            if (typeSymbol.ContainsAttribute(reference.MemoryPackableAttribute))
            {
                yield return typeSymbol;
            }
        }
    }

    public IEnumerable<ITypeSymbol> GetTypes()
        => this.types.OfType<ITypeSymbol>();
}