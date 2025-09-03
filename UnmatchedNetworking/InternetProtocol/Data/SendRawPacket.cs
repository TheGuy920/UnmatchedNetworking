using System.Buffers;
using System.Net;
using System.Net.Sockets;
using MemoryPack;
using UnmatchedNetworking.Networking;

namespace UnmatchedNetworking.InternetProtocol.Data;

public class SendRawPacket(RawPacket rawPacket, NetworkUserId targetUserId) : ISendPacket
{
    public NetworkUserId TargetUserId { get; } = targetUserId;

    public void WriteTo(Socket udpSocket, EndPoint endPoint)
        => udpSocket.SendTo(rawPacket.Data.ToArray(), endPoint);

    public void WriteTo(FastNetworkStream tcpNetworkStream)
    {
        tcpNetworkStream.Write(rawPacket.Data);
        tcpNetworkStream.Flush();
    }
}

public class SendPacketStream<T>(SerializeToStream<T> serializeToStream, T? @object, NetworkUserId targetUserId)
    : ISendPacket where T : INetworkCommand
{
    public NetworkUserId TargetUserId { get; } = targetUserId;

    public void WriteTo(Socket udpSocket, EndPoint endPoint)
    {
        // FastNetworkStream stream = FastNetworkStream.Create(udpSocket.);
        // MemoryPackSerializer.Serialize(stream, @object!.TypeId);
        // serializeToStream(@object, stream);
        // udpSocket.SendTo(stream.ToArray(), endPoint);
    }

    public void WriteTo(FastNetworkStream tcpNetworkStream)
    {
        MemoryPackSerializer.Serialize(tcpNetworkStream, @object!.TypeId);
        serializeToStream(@object, tcpNetworkStream);
        tcpNetworkStream.Flush();
    }
}

public interface ISendPacket
{
    public NetworkUserId TargetUserId { get; }

    public void WriteTo(Socket udpSocket, EndPoint endPoint);

    public void WriteTo(FastNetworkStream tcpNetworkStream);
}