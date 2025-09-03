using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;

namespace UnmatchedNetworking.InternetProtocol.Data;

public class FastNetworkStream : IBufferWriter<byte>
{
    private readonly ConcurrentQueue<ReusedBuffer> _queues = [];
    private readonly NetworkStream _stream;
    private ReusedBuffer? _lastBuffer;

    private int _totalSize;

    private FastNetworkStream(NetworkStream stream) => this._stream = stream;

    public void Advance(int count)
    {
        if (this._lastBuffer is not null)
        {
            count += 1;
            Interlocked.Add(ref this._totalSize, count);

            this._lastBuffer.UpdateCount(count);
            this._queues.Enqueue(this._lastBuffer);
        }

        this._lastBuffer = null;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (sizeHint == 0)
            return null;

        this._lastBuffer ??= ReusedBuffer.Create(sizeHint + 1);
        return this._lastBuffer.Memory;
    }

    public Span<byte> GetSpan(int sizeHint = 0)
        => this.GetMemory(sizeHint).Span;

    public static FastNetworkStream Create(NetworkStream stream) => new(stream);

    public void Flush()
    {
        var position = 0;
        var finalBuffer = ReusedBuffer.Create(this._totalSize);

        while (this._queues.TryDequeue(out ReusedBuffer rBuf))
        {
            int size = rBuf.Count;
            Buffer.BlockCopy(rBuf.Buffer, 0, finalBuffer, position, size);
            position += size;
            rBuf.Release();
        }

        finalBuffer.WriteTo(this._stream);
    }

    public ReusedBuffer Read(int offset, int size)
    {
        var buffer = ReusedBuffer.Create(size);
        int amtRead = this._stream.Read(buffer, offset, size);
        buffer.UpdateCount(amtRead);
        return buffer;
    }
}