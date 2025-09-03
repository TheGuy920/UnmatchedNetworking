using System;
using System.Buffers;
using JetBrains.Annotations;
using MemoryPack;

namespace UnmatchedNetworking.Networking;

[PublicAPI]
public static class NetworkCommand
{
    public static void Serialize<T>(T? instance, IBufferWriter<byte> stream) where T : INetworkCommand
    {
        // using BrotliCompressor compressor = new(CompressionLevel.Fastest);
        MemoryPackSerializer.Serialize(stream, instance);
        // compressor.CopyToAsync(stream);
    }

    public static void Deserialize<T>(ref T? instance, ReadOnlySpan<byte> data) where T : INetworkCommand
    {
        // using BrotliDecompressor compressor = new();
        // ReadOnlySequence<byte> decompressedBuffer = compressor.Decompress(RawData);
        // MemoryPackSerializer.Deserialize(decompressedBuffer, ref instance);
        MemoryPackSerializer.Deserialize(data, ref instance);
    }

    public static T? Deserialize<T>(ReadOnlySpan<byte> data) where T : INetworkCommand
    // using BrotliDecompressor compressor = new();
    // ReadOnlySequence<byte> decompressedBuffer = compressor.Decompress(RawData);
    // return MemoryPackSerializer.Deserialize<T>(decompressedBuffer);
        => MemoryPackSerializer.Deserialize<T>(data);
}