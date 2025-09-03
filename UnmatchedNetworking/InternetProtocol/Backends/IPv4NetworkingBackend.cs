using System;
using System.Net;
using System.Net.Sockets;
using JetBrains.Annotations;
using UnmatchedNetworking.InternetProtocol.Data;

namespace UnmatchedNetworking.InternetProtocol.Backends;

[PublicAPI]
// ReSharper disable once InconsistentNaming
public class IPv4NetworkingBackend<T> : INetworkingBackend where T : NetworkingSocket
{
    private readonly IPEndPoint _endPoint;
    private readonly NetworkingSocket _listener;

    public IPv4NetworkingBackend(ushort port, IPAddress address)
        : this(new IPEndPoint(address, port)) { }

    public IPv4NetworkingBackend(EndPoint endPoint)
        : this(endPoint as IPEndPoint ?? throw new ArgumentException("Invalid endpoint type", nameof(endPoint))) { }

    public IPv4NetworkingBackend(IPEndPoint endpoint)
    {
        this.Port = (ushort)endpoint.Port;
        this._endPoint = endpoint;
        this._listener = (T)Activator.CreateInstance(typeof(T), this._endPoint);
    }
    public ushort Port { get; }
    public bool IsConnected => this._listener.IsConnected;
    public event PacketReceiveCallback? PacketReceived;

    public void SendPacket(ISendPacket packet, NetworkMode method)
    {
        switch (method)
        {
            case NetworkMode.Reliable:
                this._listener.SendTcp(packet);
                break;
            case NetworkMode.Unreliable:
                this._listener.SendUdp(packet);
                break;
            case NetworkMode.NA:
            default:
                throw new ArgumentOutOfRangeException(nameof(method), method, null);
        }
    }

    public bool Connect()
    {
        try
        {
            this._listener.Start();
            return true;
        }
        catch (SocketException e)
        {
            // Handle the exception (e.g., log it, rethrow it, etc.)
            Console.WriteLine($"SocketException: {e.Message}");
            return false;
        }
    }

    public void Disconnect()
        => this._listener.Stop();

    public void ProcessEvents()
    {
        this._listener.ProcessEvents();

        foreach (TcpPacket packet in this._listener.ReceiveTcp())
        {
            NetworkUserId id = this._listener.GetClientId(packet.User);
            this.PacketReceived?.Invoke(id, packet.RawPacket);
            packet.Release();
        }

        foreach (UdpPacket packet in this._listener.ReceiveUdp())
        {
            NetworkUserId id = this._listener.GetClientId(packet.EndPoint);
            this.PacketReceived?.Invoke(id, packet.RawPacket);
        }
    }
}