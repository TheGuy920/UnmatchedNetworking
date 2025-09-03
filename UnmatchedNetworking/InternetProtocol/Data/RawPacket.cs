using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnmatchedNetworking.Networking;

namespace UnmatchedNetworking.InternetProtocol.Data;

[PublicAPI]
public class RawPacket(ReadOnlyMemory<byte> memory) : INetworkPacket
{
    private ReadOnlyMemory<byte> _memory = memory;

    /// <summary>
    /// </summary>
    public ReadOnlySpan<byte> Data => this._memory.Span;

    /// <summary>
    /// </summary>
    /// <param name="start"></param>
    /// <param name="length"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T Read<T>(int start, int length) where T : struct
    {
        ReadOnlyMemory<byte> dataSlice = this._memory.Slice(start, length);
        var tData = MemoryMarshal.Read<T>(dataSlice.Span);
        this._memory = this._memory.Slice(start + length);
        return tData;
    }

    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T Read<T>() where T : struct
        => this.Read<T>(0, Marshal.SizeOf<T>());

    /// <summary>
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    public static implicit operator RawPacket(ReadOnlyMemory<byte> span) => new(span);
}