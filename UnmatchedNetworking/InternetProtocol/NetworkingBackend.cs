using JetBrains.Annotations;
using UnmatchedNetworking.InternetProtocol.Data;

namespace UnmatchedNetworking.InternetProtocol;

[PublicAPI]
public interface INetworkingBackend
{
    public bool IsConnected { get; }

    public event PacketReceiveCallback PacketReceived;

    public void SendPacket(ISendPacket packet, NetworkMode method);

    public bool Connect();

    public void Disconnect();

    public void ProcessEvents();
}