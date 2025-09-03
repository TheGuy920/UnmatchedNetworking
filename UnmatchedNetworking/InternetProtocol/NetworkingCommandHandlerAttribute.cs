using System;
using JetBrains.Annotations;
using UnmatchedNetworking.Networking;

namespace UnmatchedNetworking.InternetProtocol;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false)]
public class NetworkingCommandHandlerAttribute<T> : Attribute where T : INetworkCommand;