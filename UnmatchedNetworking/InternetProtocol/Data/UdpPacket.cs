using System;
using System.Net;
using UnmatchedNetworking.Networking;

namespace UnmatchedNetworking.InternetProtocol.Data;

public record UdpPacket(IPEndPoint EndPoint, byte[] RawData) : INetworkPacket
{
    public RawPacket RawPacket { get; } = new(RawData);
    public ReadOnlySpan<byte> Data => this.RawPacket.Data;
}