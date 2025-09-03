using System;
using System.Buffers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnmatchedNetworking.InternetProtocol.Data;
using UnmatchedNetworking.Networking;

namespace UnmatchedNetworking.InternetProtocol;

public delegate void PacketReceiveCallback(NetworkUserId sender, RawPacket packet);

public delegate void CommandReceivedCallback<in T>(NetworkUserId sender, T packet) where T : INetworkCommand;

public delegate void PacketSendCallback(ISendPacket packet, NetworkMode method);

public delegate Task TaskRunner(Action action, CancellationToken token = default);

public delegate void ThreadSleeper(TimeSpan timeout);

public delegate void SerializeToStream<in T>(T? value, IBufferWriter<byte> stream) where T : INetworkCommand;

public delegate int WriteUdp(byte[] data, EndPoint endPoint);