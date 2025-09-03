using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnmatchedNetworking.InternetProtocol;

namespace UnmatchedNetworking;

[PublicAPI]
public interface INetworkingService
{
    public TimeSpan EventProcessRate { get; }

    public T? RegisterCommandHandler<T>() where T : NetworkingCommandHandler;

    public T? RegisterCommandHandler<T>(params object[]? args) where T : NetworkingCommandHandler;

    public void SetTaskRunner(TaskRunner taskRunner);

    public void Run();

    public Task RunAsync();

    public bool Stop(TimeSpan? timeout = null);

    public void SetEventProcessRate(TimeSpan eventProcessRate);
}