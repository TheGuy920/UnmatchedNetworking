using JetBrains.Annotations;
using UnmatchedNetworking.InternetProtocol.Data;

namespace UnmatchedNetworking.InternetProtocol.Backends.Steam;

[PublicAPI]
public class SteamNetworkingBackend : INetworkingBackend
{
    public bool IsConnected { get; }

    public event PacketReceiveCallback? PacketReceived;

    public void SendPacket(ISendPacket packet, NetworkMode _)
    {
        // Implementation for sending a packet
        // This is where you would use the Steamworks API to send the packet
        // For example:
        // SteamNetworking.SendCommand(packet);
    }

    public bool Connect()
        // Implementation for connecting to the Steam network
        // This is where you would use the Steamworks API to establish a connection
        // For example:
        // return SteamNetworking.Connect();
        => true; // Placeholder return value

    public void Disconnect() { }

    public void ProcessEvents()
    {
        this.ReceivePackets();
    }

    private void ReceivePackets()
    {
        // Implementation for receiving packets
        // This is where you would use the Steamworks API to receive packets
        // For example:
        // var packet = SteamNetworking.ReceivePacket();
        // PacketReceived?.Invoke(packet);
        // ReadOnlyMemory<byte> packet = new byte[1024]; // Placeholder for received packet
        // SendRawPacket sendRawPacket = new(packet, new NetworkUserId(0));
        // this.PacketReceived?.Invoke(sendRawPacket, NetworkMode.NA);
    }
}