using System;
using UnmatchedNetworking.Networking;

namespace UnmatchedNetworking.InternetProtocol.Data;

public record TcpPacket(TcpUser User, ReusedBuffer Buffer) : INetworkPacket
{
    public RawPacket RawPacket { get; } = Buffer.AsPacket;
    public ReadOnlySpan<byte> Data => this.RawPacket.Data;

    public void Release()
    {
        this.Buffer.Release();
    }
}