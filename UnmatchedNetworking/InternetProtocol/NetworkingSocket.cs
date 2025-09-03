using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnmatchedNetworking.InternetProtocol.Data;

namespace UnmatchedNetworking.InternetProtocol;

public abstract class NetworkingSocket(IPEndPoint endPoint)
{
    protected readonly IPEndPoint EndPoint = endPoint;
    protected readonly ConcurrentDictionary<NetworkUserId, TcpUser> SocketConnections = [];
    protected readonly UdpClient UdpServer = new() { ExclusiveAddressUse = false };

    public abstract bool IsConnected { get; }

    public virtual void Start()
    {
        this.UdpServer.Client.Bind(this.EndPoint);
    }

    public virtual void Stop()
    {
        this.UdpServer.Close();
    }

    public virtual IEnumerable<TcpPacket> ReceiveTcp()
    {
        foreach (TcpUser user in this.SocketConnections.Values)
        {
            if (!user.Tcp.Connected || user.Tcp.Available <= 0)
                continue;

            ReusedBuffer buffer = user.NetworkStream.Read(0, user.Tcp.Available);
            yield return new TcpPacket(user, buffer);
        }
    }

    public virtual IEnumerable<UdpPacket> ReceiveUdp()
    {
        IPEndPoint remoteEndPoint = new(IPAddress.Any, 0);
        while (this.UdpServer.Available > 0)
        {
            byte[] buffer = this.UdpServer.Receive(ref remoteEndPoint);
            yield return new UdpPacket(remoteEndPoint, buffer);
        }
    }

    public virtual void SendTcp(ISendPacket packet)
    {
        if (packet.TargetUserId == NetworkUserId.Everyone)
        {
            foreach (TcpUser? tcpUser in this.SocketConnections.Values)
                SendTcp(packet, tcpUser);

            return;
        }

        if (packet.TargetUserId == NetworkUserId.Server)
        {
            SendTcp(packet, this.SocketConnections.Values.First());
            return;
        }

        if (!this.SocketConnections.TryGetValue(packet.TargetUserId, out TcpUser user))
            return;

        SendTcp(packet, user);
    }

    private static void SendTcp(ISendPacket packet, TcpUser user)
    {
        if (!user.Tcp.Connected)
            return;

        packet.WriteTo(user.NetworkStream);
    }

    public virtual void SendUdp(ISendPacket packet)
    {
        if (!this.SocketConnections.TryGetValue(packet.TargetUserId, out TcpUser? user))
            return;

        if (!user.Tcp.Connected)
            return;

        packet.WriteTo(this.UdpServer.Client, user.Tcp.Client.RemoteEndPoint);
    }

    public virtual NetworkUserId GetClientId(TcpUser client)
        => this.SocketConnections.FirstOrDefault(c => c.Value == client).Key
           ?? throw new KeyNotFoundException("Client not found.");

    public virtual NetworkUserId GetClientId(EndPoint userEndPoint)
    {
        foreach (KeyValuePair<NetworkUserId, TcpUser> kvp in this.SocketConnections)
            if (kvp.Value.Tcp.Client.RemoteEndPoint.Equals(userEndPoint))
                return kvp.Key;

        throw new KeyNotFoundException("Client not found.");
    }

    public abstract void ProcessEvents();
}