using System.Net;
using System.Net.Sockets;
using JetBrains.Annotations;

namespace UnmatchedNetworking.InternetProtocol.Data;

[PublicAPI]
public record NetworkUserId(int Id)
{
    public static readonly NetworkUserId Everyone = new(-1);

    public static readonly NetworkUserId Server = new(0);

    public NetworkUserId(TcpClient client)
        : this(client.GetHashCode()) { }

    public NetworkUserId(IPEndPoint endPoint)
        : this(endPoint.GetHashCode()) { }
}