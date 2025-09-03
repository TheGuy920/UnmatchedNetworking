using System;

namespace UnmatchedNetworking.Networking;

public interface INetworkPacket
{
    public ReadOnlySpan<byte> Data { get; }
}