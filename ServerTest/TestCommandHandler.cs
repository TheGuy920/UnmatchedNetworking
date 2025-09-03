using UnmatchedNetworking.InternetProtocol;
using UnmatchedNetworking.InternetProtocol.Data;

namespace ServerTest;

[NetworkingCommandHandler<BenchmarkCommand>]
internal partial class BenchmarkCommandHandler
{
    public event CommandReceivedCallback<BenchmarkCommand>? OnMessage;

    public override void Process(NetworkUserId sender, BenchmarkCommand? instance)
        => this.OnMessage?.Invoke(sender, instance!);
}