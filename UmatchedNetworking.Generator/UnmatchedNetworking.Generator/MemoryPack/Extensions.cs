using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MemoryPack.Generator;

internal static class Extensions
{
    private const string UnderScorePrefix = "_";

    public static string NewLine(this IEnumerable<string> source)
        => string.Join("\n", source);

    public static bool EqualsNamespaceAndName(this ITypeSymbol? left, ITypeSymbol? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;

        INamespaceSymbol? l = left.ContainingNamespace;
        INamespaceSymbol? r = right.ContainingNamespace;
        while (l != null && r != null)
        {
            if (l.Name != r.Name) return false;

            l = l.ContainingNamespace;
            r = r.ContainingNamespace;
        }

        return left.Name == right.Name;
    }

    public static bool ContainsAttribute(this ISymbol symbol, INamedTypeSymbol attribtue)
    {
        return symbol.GetAttributes().Any(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, attribtue));
    }

    public static AttributeData? GetAttribute(this ISymbol symbol, INamedTypeSymbol attribtue)
    {
        return symbol.GetAttributes().FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, attribtue));
    }

    public static AttributeData? GetImplAttribute(this ISymbol symbol, INamedTypeSymbol implAttribtue)
    {
        return symbol.GetAttributes().FirstOrDefault(x =>
        {
            if (x.AttributeClass == null) return false;
            if (x.AttributeClass.EqualsUnconstructedGenericType(implAttribtue)) return true;

            foreach (INamedTypeSymbol? item in x.AttributeClass.GetAllBaseTypes())
            {
                if (item.EqualsUnconstructedGenericType(implAttribtue))
                {
                    return true;
                }
            }

            return false;
        });
    }

    public static IEnumerable<ISymbol> GetAllMembers(this INamedTypeSymbol symbol, bool withoutOverride = true)
    {
        // Iterate Parent -> Derived
        if (symbol.BaseType != null)
        {
            foreach (ISymbol? item in GetAllMembers(symbol.BaseType))
            {
                // override item already iterated in parent type
                if (!withoutOverride || !item.IsOverride)
                {
                    yield return item;
                }
            }
        }

        foreach (ISymbol? item in symbol.GetMembers())
        {
            if (!withoutOverride || !item.IsOverride)
            {
                yield return item;
            }
        }
    }

    public static IEnumerable<ISymbol> GetParentMembers(this INamedTypeSymbol symbol)
    {
        // Iterate Parent -> Derived
        if (symbol.BaseType != null)
        {
            foreach (ISymbol? item in GetAllMembers(symbol.BaseType))
            {
                // override item already iterated in parent type
                if (!item.IsOverride)
                {
                    yield return item;
                }
            }
        }
    }

    public static bool TryGetMemoryPackableType(this ITypeSymbol symbol, ReferenceSymbols references, out GenerateType generateType, out SerializeLayout serializeLayout)
    {
        AttributeData? memPackAttr = symbol.GetAttribute(references.MemoryPackableAttribute);
        ImmutableArray<TypedConstant>? packableCtorArgs = memPackAttr?.ConstructorArguments;
        generateType = GenerateType.Object;
        serializeLayout = SerializeLayout.Sequential;

        if (memPackAttr == null || packableCtorArgs == null)
        {
            AttributeData? memPackUnionFormatterAttr = symbol.GetAttribute(references.MemoryPackUnionFormatterAttribute);
            generateType = memPackUnionFormatterAttr != null ? GenerateType.Union : GenerateType.NoGenerate;
            serializeLayout = SerializeLayout.Sequential;
            return false;
        }

        if (packableCtorArgs.Value.Length != 0)
        {
            // MemoryPackable has three attribtues
            // [GenerateType generateType]
            // [SerializeLayout serializeLayout]
            // [GenerateType generateType, SerializeLayout serializeLayout]

            if (packableCtorArgs.Value.Length == 1)
            {
                TypedConstant ctorValue = packableCtorArgs.Value[0];

                // check which construcotr was used
                IMethodSymbol? attrConstructor = memPackAttr.AttributeConstructor;
                bool isSerializeLayout = attrConstructor!.Parameters[0].Type.Name == nameof(SerializeLayout);
                if (isSerializeLayout)
                {
                    generateType = GenerateType.Object;
                    serializeLayout = (SerializeLayout)ctorValue.Value!;
                }
                else
                {
                    generateType = (GenerateType)ctorValue.Value!;
                    serializeLayout = SerializeLayout.Sequential;
                    if (generateType is GenerateType.VersionTolerant or GenerateType.CircularReference)
                    {
                        serializeLayout = SerializeLayout.Explicit;
                    }
                }
            }
            else
            {
                generateType = (GenerateType)(packableCtorArgs.Value[0].Value ?? GenerateType.Object);
                serializeLayout = (SerializeLayout)(packableCtorArgs.Value[1].Value ?? SerializeLayout.Sequential);
            }
        }

        if (generateType == GenerateType.Object && (symbol.IsStatic || symbol.IsAbstract))
        {
            // static or abstract class is Union, set as NoGenerate
            generateType = GenerateType.Union;
            serializeLayout = SerializeLayout.Sequential;
        }

        return true;
    }

    public static bool IsMemoryPackableNoGenerate(this ITypeSymbol symbol, ReferenceSymbols references)
    {
        AttributeData? memPackAttr = symbol.GetAttribute(references.MemoryPackableAttribute);
        ImmutableArray<TypedConstant>? packableCtorArgs = memPackAttr?.ConstructorArguments;
        if (memPackAttr == null || packableCtorArgs == null)
        {
            return false;
        }

        if (packableCtorArgs.Value.Length != 0)
        {
            // MemoryPackable has three attribtues
            // [GenerateType generateType]
            // [SerializeLayout serializeLayout]
            // [GenerateType generateType, SerializeLayout serializeLayout]

            if (packableCtorArgs.Value.Length == 1)
            {
                TypedConstant ctorValue = packableCtorArgs.Value[0];

                // check which constructor was used
                IMethodSymbol? attrConstructor = memPackAttr.AttributeConstructor;
                bool isSerializeLayout = attrConstructor!.Parameters[0].Type.Name == nameof(SerializeLayout);
                if (isSerializeLayout)
                {
                    return false;
                }

                var generateType = (GenerateType)ctorValue.Value!;
                return generateType is GenerateType.NoGenerate;
            }
            else
            {
                var generateType = (GenerateType)(packableCtorArgs.Value[0].Value ?? GenerateType.Object);

                return generateType is GenerateType.NoGenerate;
            }
        }

        return false;
    }

    public static bool IsWillImplementMemoryPackUnion(this ITypeSymbol symbol, ReferenceSymbols references)
        => symbol.IsAbstract && symbol.ContainsAttribute(references.MemoryPackUnionAttribute);

    public static bool HasDuplicate<T>(this IEnumerable<T> source)
    {
        var set = new HashSet<T>();
        foreach (T item in source)
        {
            if (!set.Add(item))
            {
                return true;
            }
        }

        return false;
    }

    public static IEnumerable<INamedTypeSymbol> GetAllBaseTypes(this INamedTypeSymbol symbol)
    {
        INamedTypeSymbol? t = symbol.BaseType;
        while (t != null)
        {
            yield return t;
            t = t.BaseType;
        }
    }

    internal static string ToFullyQualifiedFormatDisplayString(this ITypeSymbol typeSymbol)
        => typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public static string FullyQualifiedToString(this ISymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public static bool EqualsUnconstructedGenericType(this INamedTypeSymbol left, INamedTypeSymbol right)
    {
        INamedTypeSymbol l = left.IsGenericType ? left.ConstructUnboundGenericType() : left;
        INamedTypeSymbol r = right.IsGenericType ? right.ConstructUnboundGenericType() : right;
        return SymbolEqualityComparer.Default.Equals(l, r);
    }

    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) => DistinctBy(source, keySelector, null);

    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        => DistinctByIterator(source, keySelector, comparer);

    private static IEnumerable<TSource> DistinctByIterator<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
    {
        using IEnumerator<TSource> enumerator = source.GetEnumerator();

        if (enumerator.MoveNext())
        {
            var set = new HashSet<TKey>(comparer);
            do
            {
                TSource element = enumerator.Current;
                if (set.Add(keySelector(element)))
                {
                    yield return element;
                }
            } while (enumerator.MoveNext());
        }
    }

    public static bool TryGetConstructorParameter(this IMethodSymbol constructor, ISymbol member, out IParameterSymbol? constructorParameter)
    {
        constructorParameter = GetConstructorParameter(constructor, member.Name);
        if (constructorParameter == null && member.Name.StartsWith(UnderScorePrefix))
        {
            constructorParameter = GetConstructorParameter(constructor, member.Name.Substring(UnderScorePrefix.Length));
        }

        return constructorParameter != null;

        static IParameterSymbol? GetConstructorParameter(IMethodSymbol constructor, string name) => constructor.Parameters.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public static bool ContainsConstructorParameter(this IEnumerable<MemberMeta> members, IParameterSymbol constructorParameter) =>
        members.Any(x =>
            x.IsConstructorParameter &&
            string.Equals(constructorParameter.Name, x.ConstructorParameterName, StringComparison.OrdinalIgnoreCase));
}