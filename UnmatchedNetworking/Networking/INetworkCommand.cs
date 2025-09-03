using System;
using JetBrains.Annotations;

namespace UnmatchedNetworking.Networking;

[PublicAPI]
public interface INetworkCommand : INetworkPacket
{
    public Guid TypeId { get; }

    public Guid InstanceId { get; }
}