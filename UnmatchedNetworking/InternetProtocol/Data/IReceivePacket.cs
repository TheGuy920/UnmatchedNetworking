using System;

namespace UnmatchedNetworking.InternetProtocol.Data;

public class ReceivePacket
{
    public readonly RawPacket RawPacket;

    public ReceivePacket(ReadOnlyMemory<byte> data)
        => this.RawPacket = new RawPacket(data);

    public ReceivePacket(RawPacket packet)
        => this.RawPacket = packet;
}