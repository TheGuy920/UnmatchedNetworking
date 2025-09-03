using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MemoryPack.Generator;

public class ReferenceSymbols
{
    public ReferenceSymbols(Compilation compilation)
    {
        this.Compilation = compilation;

        // MemoryPack
        this.MemoryPackableAttribute = this.GetTypeByMetadataName(MemoryPackGenerator.MemoryPackableAttributeFullName);
        this.MemoryPackUnionAttribute = this.GetTypeByMetadataName("MemoryPack.MemoryPackUnionAttribute");
        this.MemoryPackUnionFormatterAttribute = this.GetTypeByMetadataName("MemoryPack.MemoryPackUnionFormatterAttribute");
        this.MemoryPackConstructorAttribute = this.GetTypeByMetadataName("MemoryPack.MemoryPackConstructorAttribute");
        this.MemoryPackAllowSerializeAttribute = this.GetTypeByMetadataName("MemoryPack.MemoryPackAllowSerializeAttribute");
        this.MemoryPackOrderAttribute = this.GetTypeByMetadataName("MemoryPack.MemoryPackOrderAttribute");
        this.MemoryPackCustomFormatterAttribute = compilation.GetTypeByMetadataName("MemoryPack.MemoryPackCustomFormatterAttribute`1")?.ConstructUnboundGenericType();
        this.MemoryPackCustomFormatter2Attribute = compilation.GetTypeByMetadataName("MemoryPack.MemoryPackCustomFormatterAttribute`2")?.ConstructUnboundGenericType();
        this.MemoryPackIgnoreAttribute = this.GetTypeByMetadataName("MemoryPack.MemoryPackIgnoreAttribute");
        this.MemoryPackIncludeAttribute = this.GetTypeByMetadataName("MemoryPack.MemoryPackIncludeAttribute");
        this.MemoryPackOnSerializingAttribute = this.GetTypeByMetadataName("MemoryPack.MemoryPackOnSerializingAttribute");
        this.MemoryPackOnSerializedAttribute = this.GetTypeByMetadataName("MemoryPack.MemoryPackOnSerializedAttribute");
        this.MemoryPackOnDeserializingAttribute = this.GetTypeByMetadataName("MemoryPack.MemoryPackOnDeserializingAttribute");
        this.MemoryPackOnDeserializedAttribute = this.GetTypeByMetadataName("MemoryPack.MemoryPackOnDeserializedAttribute");
        this.SkipOverwriteDefaultAttribute = this.GetTypeByMetadataName("MemoryPack.SuppressDefaultInitializationAttribute");
        this.GenerateTypeScriptAttribute = this.GetTypeByMetadataName(MemoryPackGenerator.GenerateTypeScriptAttributeFullName);
        this.IMemoryPackable = this.GetTypeByMetadataName("MemoryPack.IMemoryPackable`1").ConstructUnboundGenericType();
        this.KnownTypes = new WellKnownTypes(this);
    }
    public Compilation Compilation { get; }

    public INamedTypeSymbol MemoryPackableAttribute { get; }
    public INamedTypeSymbol MemoryPackUnionAttribute { get; }
    public INamedTypeSymbol MemoryPackUnionFormatterAttribute { get; }
    public INamedTypeSymbol MemoryPackConstructorAttribute { get; }
    public INamedTypeSymbol MemoryPackAllowSerializeAttribute { get; }
    public INamedTypeSymbol MemoryPackOrderAttribute { get; }
    public INamedTypeSymbol? MemoryPackCustomFormatterAttribute { get; } // Unity is null.
    public INamedTypeSymbol? MemoryPackCustomFormatter2Attribute { get; } // Unity is null.
    public INamedTypeSymbol MemoryPackIgnoreAttribute { get; }
    public INamedTypeSymbol MemoryPackIncludeAttribute { get; }
    public INamedTypeSymbol MemoryPackOnSerializingAttribute { get; }
    public INamedTypeSymbol MemoryPackOnSerializedAttribute { get; }
    public INamedTypeSymbol MemoryPackOnDeserializingAttribute { get; }
    public INamedTypeSymbol MemoryPackOnDeserializedAttribute { get; }
    public INamedTypeSymbol SkipOverwriteDefaultAttribute { get; }
    public INamedTypeSymbol GenerateTypeScriptAttribute { get; }
    public INamedTypeSymbol IMemoryPackable { get; }

    public WellKnownTypes KnownTypes { get; }

    private INamedTypeSymbol GetTypeByMetadataName(string metadataName)
    {
        INamedTypeSymbol? symbol = this.Compilation.GetTypeByMetadataName(metadataName);
        if (symbol == null)
        {
            throw new InvalidOperationException($"Type {metadataName} is not found in compilation.");
        }

        return symbol;
    }

    // UnamnaagedType no need.
    public class WellKnownTypes
    {
        // netstandard2.0 source generator has there reference so use string instead...
        //public INamedTypeSymbol System_Memory_T { get; }
        //public INamedTypeSymbol System_ReadOnlyMemory_T { get; }
        //public INamedTypeSymbol System_Buffers_ReadOnlySequence_T { get; }
        //public INamedTypeSymbol System_Collections_Generic_PriorityQueue_T { get; }
        private const string System_Memory_T = "global::System.Memory<>";
        private const string System_ReadOnlyMemory_T = "global::System.ReadOnlyMemory<>";
        private const string System_Buffers_ReadOnlySequence_T = "global::System.Buffers.ReadOnlySequence<>";
        private const string System_Collections_Generic_PriorityQueue_T = "global::System.Collections.Generic.PriorityQueue<,>";

        private static readonly Dictionary<string, string> knownGenericTypes = new()
        {
            // ArrayFormatters
            { "System.ArraySegment<>", "global::MemoryPack.Formatters.ArraySegmentFormatter<TREPLACE>" },
            { "System.Memory<>", "global::MemoryPack.Formatters.MemoryFormatter<TREPLACE>" },
            { "System.ReadOnlyMemory<>", "global::MemoryPack.Formatters.ReadOnlyMemoryFormatter<TREPLACE>" },
            { "System.Buffers.ReadOnlySequence<>", "global::MemoryPack.Formatters.ReadOnlySequenceFormatter<TREPLACE>" },

            // CollectionFormatters
            { "System.Collections.Generic.List<>", "global::MemoryPack.Formatters.ListFormatter<TREPLACE>" },
            { "System.Collections.Generic.Stack<>", "global::MemoryPack.Formatters.StackFormatter<TREPLACE>" },
            { "System.Collections.Generic.Queue<>", "global::MemoryPack.Formatters.QueueFormatter<TREPLACE>" },
            { "System.Collections.Generic.LinkedList<>", "global::MemoryPack.Formatters.LinkedListFormatter<TREPLACE>" },
            { "System.Collections.Generic.HashSet<>", "global::MemoryPack.Formatters.HashSetFormatter<TREPLACE>" },
            { "System.Collections.Generic.SortedSet<>", "global::MemoryPack.Formatters.SortedSetFormatter<TREPLACE>" },
            { "System.Collections.Generic.PriorityQueue<,>", "global::MemoryPack.Formatters.PriorityQueueFormatter<TREPLACE>" },
            { "System.Collections.ObjectModel.ObservableCollection<>", "global::MemoryPack.Formatters.ObservableCollectionFormatter<TREPLACE>" },
            { "System.Collections.ObjectModel.Collection<>", "global::MemoryPack.Formatters.CollectionFormatter<TREPLACE>" },
            { "System.Collections.Concurrent.ConcurrentQueue<>", "global::MemoryPack.Formatters.ConcurrentQueueFormatter<TREPLACE>" },
            { "System.Collections.Concurrent.ConcurrentStack<>", "global::MemoryPack.Formatters.ConcurrentStackFormatter<TREPLACE>" },
            { "System.Collections.Concurrent.ConcurrentBag<>", "global::MemoryPack.Formatters.ConcurrentBagFormatter<TREPLACE>" },
            { "System.Collections.Generic.Dictionary<,>", "global::MemoryPack.Formatters.DictionaryFormatter<TREPLACE>" },
            { "System.Collections.Generic.SortedDictionary<,>", "global::MemoryPack.Formatters.SortedDictionaryFormatter<TREPLACE>" },
            { "System.Collections.Generic.SortedList<,>", "global::MemoryPack.Formatters.SortedListFormatter<TREPLACE>" },
            { "System.Collections.Concurrent.ConcurrentDictionary<,>", "global::MemoryPack.Formatters.ConcurrentDictionaryFormatter<TREPLACE>" },
            { "System.Collections.ObjectModel.ReadOnlyCollection<>", "global::MemoryPack.Formatters.ReadOnlyCollectionFormatter<TREPLACE>" },
            { "System.Collections.ObjectModel.ReadOnlyObservableCollection<>", "global::MemoryPack.Formatters.ReadOnlyObservableCollectionFormatter<TREPLACE>" },
            { "System.Collections.Concurrent.BlockingCollection<>", "global::MemoryPack.Formatters.BlockingCollectionFormatter<TREPLACE>" },

            // ImmutableCollectionFormatters
            { "System.Collections.Immutable.ImmutableArray<>", "global::MemoryPack.Formatters.ImmutableArrayFormatter<TREPLACE>" },
            { "System.Collections.Immutable.ImmutableList<>", "global::MemoryPack.Formatters.ImmutableListFormatter<TREPLACE>" },
            { "System.Collections.Immutable.ImmutableQueue<>", "global::MemoryPack.Formatters.ImmutableQueueFormatter<TREPLACE>" },
            { "System.Collections.Immutable.ImmutableStack<>", "global::MemoryPack.Formatters.ImmutableStackFormatter<TREPLACE>" },
            { "System.Collections.Immutable.ImmutableDictionary<,>", "global::MemoryPack.Formatters.ImmutableDictionaryFormatter<TREPLACE>" },
            { "System.Collections.Immutable.ImmutableSortedDictionary<,>", "global::MemoryPack.Formatters.ImmutableSortedDictionaryFormatter<TREPLACE>" },
            { "System.Collections.Immutable.ImmutableSortedSet<>", "global::MemoryPack.Formatters.ImmutableSortedSetFormatter<TREPLACE>" },
            { "System.Collections.Immutable.ImmutableHashSet<>", "global::MemoryPack.Formatters.ImmutableHashSetFormatter<TREPLACE>" },
            { "System.Collections.Immutable.IImmutableList<>", "global::MemoryPack.Formatters.InterfaceImmutableListFormatter<TREPLACE>" },
            { "System.Collections.Immutable.IImmutableQueue<>", "global::MemoryPack.Formatters.InterfaceImmutableQueueFormatter<TREPLACE>" },
            { "System.Collections.Immutable.IImmutableStack<>", "global::MemoryPack.Formatters.InterfaceImmutableStackFormatter<TREPLACE>" },
            { "System.Collections.Immutable.IImmutableDictionary<,>", "global::MemoryPack.Formatters.InterfaceImmutableDictionaryFormatter<TREPLACE>" },
            { "System.Collections.Immutable.IImmutableSet<>", "global::MemoryPack.Formatters.InterfaceImmutableSetFormatter<TREPLACE>" },

            // InterfaceCollectionFormatters
            { "System.Collections.Generic.IEnumerable<>", "global::MemoryPack.Formatters.InterfaceEnumerableFormatter<TREPLACE>" },
            { "System.Collections.Generic.ICollection<>", "global::MemoryPack.Formatters.InterfaceCollectionFormatter<TREPLACE>" },
            { "System.Collections.Generic.IReadOnlyCollection<>", "global::MemoryPack.Formatters.InterfaceReadOnlyCollectionFormatter<TREPLACE>" },
            { "System.Collections.Generic.IList<>", "global::MemoryPack.Formatters.InterfaceListFormatter<TREPLACE>" },
            { "System.Collections.Generic.IReadOnlyList<>", "global::MemoryPack.Formatters.InterfaceReadOnlyListFormatter<TREPLACE>" },
            { "System.Collections.Generic.IDictionary<,>", "global::MemoryPack.Formatters.InterfaceDictionaryFormatter<TREPLACE>" },
            { "System.Collections.Generic.IReadOnlyDictionary<,>", "global::MemoryPack.Formatters.InterfaceReadOnlyDictionaryFormatter<TREPLACE>" },
            { "System.Linq.ILookup<,>", "global::MemoryPack.Formatters.InterfaceLookupFormatter<TREPLACE>" },
            { "System.Linq.IGrouping<,>", "global::MemoryPack.Formatters.InterfaceGroupingFormatter<TREPLACE>" },
            { "System.Collections.Generic.ISet<>", "global::MemoryPack.Formatters.InterfaceSetFormatter<TREPLACE>" },
            { "System.Collections.Generic.IReadOnlySet<>", "global::MemoryPack.Formatters.InterfaceReadOnlySetFormatter<TREPLACE>" },

            { "System.Collections.Generic.KeyValuePair<,>", "global::MemoryPack.Formatters.KeyValuePairFormatter<TREPLACE>" },
            { "System.Lazy<>", "global::MemoryPack.Formatters.LazyFormatter<TREPLACE>" },

            // TupleFormatters
            { "System.Tuple<>", "global::MemoryPack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,>", "global::MemoryPack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,>", "global::MemoryPack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,,>", "global::MemoryPack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,,,>", "global::MemoryPack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,,,,>", "global::MemoryPack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,,,,,>", "global::MemoryPack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,,,,,,>", "global::MemoryPack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.ValueTuple<>", "global::MemoryPack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,>", "global::MemoryPack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,>", "global::MemoryPack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,,>", "global::MemoryPack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,,,>", "global::MemoryPack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,,,,>", "global::MemoryPack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,,,,,>", "global::MemoryPack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,,,,,,>", "global::MemoryPack.Formatters.ValueTupleFormatter<TREPLACE>" }
        };

        private readonly HashSet<ITypeSymbol> knownTypes;
        private readonly ReferenceSymbols parent;

        public WellKnownTypes(ReferenceSymbols parent)
        {
            this.parent = parent;
            this.System_Collections_Generic_IEnumerable_T = this.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1").ConstructUnboundGenericType();
            this.System_Collections_Generic_ICollection_T = this.GetTypeByMetadataName("System.Collections.Generic.ICollection`1").ConstructUnboundGenericType();
            this.System_Collections_Generic_ISet_T = this.GetTypeByMetadataName("System.Collections.Generic.ISet`1").ConstructUnboundGenericType();
            this.System_Collections_Generic_IDictionary_T = this.GetTypeByMetadataName("System.Collections.Generic.IDictionary`2").ConstructUnboundGenericType();
            this.System_Collections_Generic_List_T = this.GetTypeByMetadataName("System.Collections.Generic.List`1").ConstructUnboundGenericType();
            this.System_Guid = this.GetTypeByMetadataName("System.Guid");
            this.System_Version = this.GetTypeByMetadataName("System.Version");
            this.System_Uri = this.GetTypeByMetadataName("System.Uri");
            this.System_Numerics_BigInteger = this.GetTypeByMetadataName("System.Numerics.BigInteger");
            this.System_TimeZoneInfo = this.GetTypeByMetadataName("System.TimeZoneInfo");
            this.System_Collections_BitArray = this.GetTypeByMetadataName("System.Collections.BitArray");
            this.System_Text_StringBuilder = this.GetTypeByMetadataName("System.Text.StringBuilder");
            this.System_Type = this.GetTypeByMetadataName("System.Type");
            this.System_Globalization_CultureInfo = this.GetTypeByMetadataName("System.Globalization.CultureInfo");
            this.System_Lazy_T = this.GetTypeByMetadataName("System.Lazy`1").ConstructUnboundGenericType();
            this.System_Collections_Generic_KeyValuePair_T = this.GetTypeByMetadataName("System.Collections.Generic.KeyValuePair`2").ConstructUnboundGenericType();
            this.System_Nullable_T = this.GetTypeByMetadataName("System.Nullable`1").ConstructUnboundGenericType();
            //System_Memory_T = GetTypeByMetadataName("System.Memory").ConstructUnboundGenericType();
            //System_ReadOnlyMemory_T = GetTypeByMetadataName("System.ReadOnlyMemory").ConstructUnboundGenericType();
            //System_Buffers_ReadOnlySequence_T = GetTypeByMetadataName("System.Buffers.ReadOnlySequence").ConstructUnboundGenericType();
            //System_Collections_Generic_PriorityQueue_T = GetTypeByMetadataName("System.Collections.Generic.PriorityQueue").ConstructUnboundGenericType();

            this.System_DateTime = this.GetTypeByMetadataName("System.DateTime");
            this.System_DateTimeOffset = this.GetTypeByMetadataName("System.DateTimeOffset");
            this.System_Runtime_InteropServices_StructLayout = this.GetTypeByMetadataName("System.Runtime.InteropServices.StructLayoutAttribute");

            this.knownTypes = new HashSet<ITypeSymbol>(new[]
            {
                this.System_Collections_Generic_IEnumerable_T,
                this.System_Collections_Generic_ICollection_T,
                this.System_Collections_Generic_ISet_T,
                this.System_Collections_Generic_IDictionary_T,
                this.System_Version,
                this.System_Uri,
                this.System_Numerics_BigInteger,
                this.System_TimeZoneInfo,
                this.System_Collections_BitArray,
                this.System_Text_StringBuilder,
                this.System_Type,
                this.System_Globalization_CultureInfo,
                this.System_Lazy_T,
                this.System_Collections_Generic_KeyValuePair_T,
                this.System_Nullable_T
                //System_Memory_T,
                //System_ReadOnlyMemory_T,
                //System_Buffers_ReadOnlySequence_T,
                //System_Collections_Generic_PriorityQueue_T
            }, SymbolEqualityComparer.Default);
        }

        public INamedTypeSymbol System_Collections_Generic_IEnumerable_T { get; }
        public INamedTypeSymbol System_Collections_Generic_ICollection_T { get; }
        public INamedTypeSymbol System_Collections_Generic_ISet_T { get; }
        public INamedTypeSymbol System_Collections_Generic_IDictionary_T { get; }
        public INamedTypeSymbol System_Collections_Generic_List_T { get; }

        public INamedTypeSymbol System_Guid { get; }
        public INamedTypeSymbol System_Version { get; }
        public INamedTypeSymbol System_Uri { get; }

        public INamedTypeSymbol System_Numerics_BigInteger { get; }
        public INamedTypeSymbol System_TimeZoneInfo { get; }
        public INamedTypeSymbol System_Collections_BitArray { get; }
        public INamedTypeSymbol System_Text_StringBuilder { get; }
        public INamedTypeSymbol System_Type { get; }
        public INamedTypeSymbol System_Globalization_CultureInfo { get; }
        public INamedTypeSymbol System_Lazy_T { get; }
        public INamedTypeSymbol System_Collections_Generic_KeyValuePair_T { get; }
        public INamedTypeSymbol System_Nullable_T { get; }

        public INamedTypeSymbol System_DateTime { get; }
        public INamedTypeSymbol System_DateTimeOffset { get; }
        public INamedTypeSymbol System_Runtime_InteropServices_StructLayout { get; }

        public bool Contains(ITypeSymbol symbol)
        {
            ITypeSymbol constructedSymbol = symbol;
            if (symbol is INamedTypeSymbol nts && nts.IsGenericType)
            {
                symbol = nts.ConstructUnboundGenericType();
            }

            bool contains1 = this.knownTypes.Contains(symbol);
            if (contains1) return true;

            string fullyQualifiedString = symbol.FullyQualifiedToString();
            if (fullyQualifiedString is System_Memory_T or System_ReadOnlyMemory_T or System_Buffers_ReadOnlySequence_T or System_Collections_Generic_PriorityQueue_T)
            {
                return true;
            }

            // tuple
            if (fullyQualifiedString.StartsWith("global::System.Tuple<") || fullyQualifiedString.StartsWith("global::System.ValueTuple<"))
            {
                return true;
            }

            // Most collections are basically serializable, wellknown
            bool isIterable = constructedSymbol.AllInterfaces.Any(x => x.EqualsUnconstructedGenericType(this.System_Collections_Generic_IEnumerable_T));
            if (isIterable)
            {
                return true;
            }

            return false;
        }

        public string? GetNonDefaultFormatterName(ITypeSymbol? type)
        {
            if (type == null) return null;

            if (type.TypeKind == TypeKind.Enum)
            {
                return $"global::MemoryPack.Formatters.UnmanagedFormatter<{type.FullyQualifiedToString()}>";
            }

            if (type.TypeKind == TypeKind.Array)
            {
                if (type is IArrayTypeSymbol array)
                {
                    if (array.IsSZArray)
                    {
                        return $"global::MemoryPack.Formatters.ArrayFormatter<{array.ElementType.FullyQualifiedToString()}>";
                    }

                    if (array.Rank == 2)
                    {
                        return $"global::MemoryPack.Formatters.TwoDimensionalArrayFormatter<{array.ElementType.FullyQualifiedToString()}>";
                    }

                    if (array.Rank == 3)
                    {
                        return $"global::MemoryPack.Formatters.ThreeDimensionalArrayFormatter<{array.ElementType.FullyQualifiedToString()}>";
                    }

                    if (array.Rank == 4)
                    {
                        return $"global::MemoryPack.Formatters.FourDimensionalArrayFormatter<{array.ElementType.FullyQualifiedToString()}>";
                    }
                }

                return null;
            }

            if (type is not INamedTypeSymbol named) return null;

            if (!named.IsGenericType) return null;

            INamedTypeSymbol genericType = named.ConstructUnboundGenericType();
            string genericTypeString = genericType.ToDisplayString();
            string fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            // var isOpenGenericType = type.TypeArguments.Any(x => x is ITypeParameterSymbol);

            // nullable
            if (genericTypeString == "T?")
            {
                ITypeSymbol firstTypeArgument = named.TypeArguments[0];
                string f = "global::MemoryPack.Formatters.NullableFormatter<" + firstTypeArgument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ">";
                return f;
            }

            // known types
            if (knownGenericTypes.TryGetValue(genericTypeString, out string? formatter))
            {
                string typeArgs = string.Join(", ", named.TypeArguments.Select(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                string f = formatter.Replace("TREPLACE", typeArgs);
                return f;
            }

            return null;
        }

        private INamedTypeSymbol GetTypeByMetadataName(string metadataName) => this.parent.GetTypeByMetadataName(metadataName);
    }
}