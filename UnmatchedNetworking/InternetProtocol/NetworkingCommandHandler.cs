using System;
using JetBrains.Annotations;
using UnmatchedNetworking.InternetProtocol.Data;
using UnmatchedNetworking.Networking;

namespace UnmatchedNetworking.InternetProtocol;

[PublicAPI]
public abstract class NetworkingCommandHandler
{
    /// <summary>
    /// </summary>
    public abstract Guid TypeId { get; }

    /// <summary>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="data"></param>
    public abstract void Process(NetworkUserId sender, INetworkPacket data);

    /// <summary>
    /// </summary>
    /// <param name="packet"></param>
    /// <param name="mode"></param>
    protected void SendPacket(ISendPacket packet, NetworkMode mode)
        => this.OnSendPacket?.Invoke(packet, mode);

    /// <summary>
    /// </summary>
    internal event PacketSendCallback? OnSendPacket;
}

[PublicAPI]
public abstract class NetworkingCommandHandler<T> : NetworkingCommandHandler where T : INetworkCommand
{
    /// <summary>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="instance"></param>
    public abstract void Process(NetworkUserId sender, T? instance);

    /// <summary>
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="target"></param>
    /// <param name="mode"></param>
    public void SendCommand(T instance, NetworkUserId target, NetworkMode mode)
    {
        SendPacketStream<T> packet = new(NetworkCommand.Serialize, instance, target);
        this.SendPacket(packet, mode);
    }
}