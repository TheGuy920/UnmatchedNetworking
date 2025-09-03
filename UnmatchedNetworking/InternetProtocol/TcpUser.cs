using System.Net.Sockets;
using JetBrains.Annotations;
using UnmatchedNetworking.InternetProtocol.Data;

namespace UnmatchedNetworking.InternetProtocol;

[PublicAPI]
public class TcpUser(TcpClient tcp)
{
    public readonly TcpClient Tcp = tcp;
    public FastNetworkStream NetworkStream => FastNetworkStream.Create(this.Tcp.GetStream());
}