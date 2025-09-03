using System;
using JetBrains.Annotations;

namespace UnmatchedNetworking.Networking;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, Inherited = false)]
public class NetworkCommandAttribute : Attribute
{
    public NetworkCommandAttribute(string id)
        => this.Id = Guid.Parse(id).ToString("D");

    public NetworkCommandAttribute(Guid id)
        => this.Id = id.ToString("D");
    public string Id { get; set; }
}