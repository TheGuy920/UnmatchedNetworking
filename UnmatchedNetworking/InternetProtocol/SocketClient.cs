using System.Net;
using System.Net.Sockets;
using JetBrains.Annotations;
using UnmatchedNetworking.InternetProtocol.Data;

namespace UnmatchedNetworking.InternetProtocol;

[PublicAPI]
public class SocketClient(IPEndPoint endPoint) : NetworkingSocket(endPoint)
{
    private readonly TcpClient _tcpClient = new() { ExclusiveAddressUse = false };
    public override bool IsConnected => this._tcpClient.Connected;
    public override void Start()
    {
        this._tcpClient.Connect(this.EndPoint);
        NetworkUserId clientId = new(0);
        this.SocketConnections.TryAdd(clientId, new TcpUser(this._tcpClient));
        base.Start();
    }

    public override void Stop()
    {
        this._tcpClient.Close();
        base.Stop();
    }

    public override void ProcessEvents()
    {
        // nothing to do :(
    }
}