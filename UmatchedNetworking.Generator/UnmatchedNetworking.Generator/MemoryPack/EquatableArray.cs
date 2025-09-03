using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MemoryPack.Generator;

public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly T[]? array;

    public EquatableArray() // for collection literal []
        => this.array = [];

    public EquatableArray(T[] array)
        => this.array = array;

    public static implicit operator EquatableArray<T>(T[] array)
        => new(array);

    public ref readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref this.array![index];
    }

    public int Length => this.array!.Length;

    public ReadOnlySpan<T> AsSpan()
        => this.array.AsSpan();

    public ReadOnlySpan<T>.Enumerator GetEnumerator()
        => this.AsSpan().GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => this.array.AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => this.array.AsEnumerable().GetEnumerator();

    public bool Equals(EquatableArray<T> other)
        => this.AsSpan().SequenceEqual(other.AsSpan());
}