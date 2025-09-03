using System.Net;
using System.Net.Sockets;
using JetBrains.Annotations;
using UnmatchedNetworking.InternetProtocol.Data;

namespace UnmatchedNetworking.InternetProtocol;

[PublicAPI]
public class SocketServer(IPEndPoint endPoint) : NetworkingSocket(endPoint)
{
    private readonly TcpListener _tcpServer = new(endPoint) { ExclusiveAddressUse = false, Server = { ReceiveBufferSize = 12_288 }};
    private bool _isConnected;

    public override bool IsConnected => this._isConnected;

    public override void Start()
    {
        this._tcpServer.Start();
        base.Start();

        this._isConnected = true;
    }

    public override void Stop()
    {
        this._tcpServer.Stop();
        base.Stop();
    }

    private void AcceptTcpConnections()
    {
        while (this._tcpServer.Pending())
        {
            TcpClient client = this._tcpServer.AcceptTcpClient();
            NetworkUserId clientId = new(client);
            this.SocketConnections.TryAdd(clientId, new TcpUser(client));
        }
    }

    public override void ProcessEvents()
    {
        this.AcceptTcpConnections();
    }
}