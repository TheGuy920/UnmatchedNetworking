using System;
using System.Buffers;
using System.IO;
using System.Threading;
using JetBrains.Annotations;

namespace UnmatchedNetworking.InternetProtocol.Data;

[PublicAPI]
public class ReusedBuffer : IDisposable
{
    private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Shared;
    public readonly byte[] Buffer;
    private RawPacket? _asPacket;

    private int _count;
    private int _realSize = -1;

    private ReusedBuffer(byte[] buffer, int realSize)
    {
        this.Buffer = buffer;
        this._count = realSize;
    }

    public int Count => this._realSize >= 0 ? this._realSize : this._count;
    public Span<byte> Span => this.Buffer.AsSpan(0, this.Count);
    public Memory<byte> Memory => this.Buffer.AsMemory(0, this.Count);

    public RawPacket AsPacket
        => this._asPacket ??= new RawPacket(this.Memory);

    public void Dispose()
    {
        if (this.Buffer.Length > 0)
            Pool.Return(this.Buffer);
    }

    public static ReusedBuffer Create(int size)
    {
        byte[] buffer = Pool.Rent(size);
        return new ReusedBuffer(buffer, size);
    }

    public void UpdateCount(int count)
    {
        if (count > this.Buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be greater than buffer length.");

        Interlocked.Add(ref this._realSize, count);
    }

    public void WriteTo(Stream destination)
    {
        destination.Write(this.Buffer, 0, this.Count);
        this.Dispose();
    }

    public void Release()
        => this.Dispose();

    public static implicit operator byte[](ReusedBuffer buffer) => buffer.Buffer;
}