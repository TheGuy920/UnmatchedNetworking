using System.Collections.Concurrent;
using System.Net;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using UnmatchedNetworking;
using UnmatchedNetworking.InternetProtocol;
using UnmatchedNetworking.InternetProtocol.Backends;
using UnmatchedNetworking.InternetProtocol.Data;

namespace ServerTest;

internal static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== UnmatchedNetworking Performance Benchmark ===");
        Console.WriteLine("Running comprehensive benchmarks with BenchmarkDotNet...\n");

        // Run the benchmarks with BenchmarkDotNet
        ManualConfig config = ManualConfig.Create(DefaultConfig.Instance)
                                          .AddDiagnoser(MemoryDiagnoser.Default) // Memory allocation tracking
                                          .AddDiagnoser(ThreadingDiagnoser.Default) // Thread count tracking
                                          .AddHardwareCounters(HardwareCounter.BranchMispredictions,
                                              HardwareCounter.BranchInstructions,
                                              HardwareCounter.CacheMisses)
                                          .AddJob(Job.Default.WithRuntime(CoreRuntime.Core80));

        BenchmarkRunner.Run<NetworkingBenchmarks>(config);
    }
}

[MemoryDiagnoser]
[ThreadingDiagnoser]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
public class NetworkingBenchmarks
{
    private const int DefaultPort = 12_549;
    private readonly ConcurrentBag<TimeSpan> _roundTripLatencies = [];
    private readonly ConcurrentDictionary<Guid, MessageData> _sentMessages = [];
    private CancellationTokenSource _cancellationSource = null!;
    private List<NetworkingService> _clients = null!;

    // Infrastructure
    private IPEndPoint _endPoint = null!;
    private NetworkingService _server = null!;

    // Test configuration parameters
    [Params(64, 1024, 8192)] public int MessageSize { get; set; }

    [Params(10, 100)] public int MessageCount { get; set; }

    [Params(5, 15)] public int ConcurrentClients { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        IPAddress ip = LocalMachine.GetLocalIpAddress();
        this._endPoint = new IPEndPoint(ip, DefaultPort);
        Console.WriteLine($"Using endpoint: {this._endPoint}");
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        // Clean up all resources
        this._cancellationSource?.Cancel();
        this._cancellationSource?.Dispose();

        TimeSpan averageLatency = TimeSpan.FromTicks((long)this._roundTripLatencies.Average(latency => latency.Ticks));
        Console.WriteLine($"Average round-trip latency: {averageLatency.TotalMilliseconds} ms");

        double speed = this.MessageCount * 2 / averageLatency.TotalSeconds;
        Console.WriteLine($"Average speed: {speed} bytes/sec");
    }

    [IterationSetup]
    public void IterationSetup()
    {
        this._cancellationSource = new CancellationTokenSource();

        // Create and start the server
        this._server = NetworkingService.Create<IPv4NetworkingBackend<SocketServer>>(this._endPoint);
        var serverHandler = this._server.RegisterCommandHandler<BenchmarkCommandHandler>();

        // Configure server message handler - echo back all received messages for latency tests
        serverHandler.OnMessage += (sender, benchCommand) =>
        {
            // Echo the message back to sender for round-trip measurements
            serverHandler.SendCommand(benchCommand, sender, benchCommand.UseReliableMode ? NetworkMode.Reliable : NetworkMode.Unreliable);
        };

        this._server.RunAsync();
        this._server.WaitUntilConnected();

        // Create clients
        this._clients = [];
        for (var i = 0; i < this.ConcurrentClients; i++)
        {
            var client = NetworkingService.Create<IPv4NetworkingBackend<SocketClient>>(this._endPoint);
            var clientHandler = client.RegisterCommandHandler<BenchmarkCommandHandler>()!;

            // Configure client to measure round trip time
            clientHandler.OnMessage += (_, benchCommand) =>
            {
                if (!this._sentMessages.TryRemove(benchCommand.MessageId, out MessageData msgData))
                    return;

                msgData.Handle.Set();
                TimeSpan latency = DateTime.UtcNow - msgData.Timestamp;
                this._roundTripLatencies.Add(latency);
            };

            client.RunAsync();
            client.WaitUntilConnected();
            this._clients.Add(client);
        }

        this._roundTripLatencies.Clear();
        this._sentMessages.Clear();
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        // Stop all clients first
        foreach (NetworkingService client in this._clients)
            client.Stop();

        // Then stop the server
        this._server.Stop();

        // Wait a bit to make sure all are stopped
        Thread.Sleep(100);
    }

    [Benchmark]
    public async Task BounceReliableBenchmark()
    {
        await this.RunTest(NetworkMode.Reliable);
    }

    private async Task RunTest(NetworkMode networkMode)
    {
        await Task.WhenAll(this._clients.Select(client => client.RegisterCommandHandler<BenchmarkCommandHandler>()).Select(clientHandler => Task.Run(() =>
        {
            var latencyHandle = new ManualResetEventSlim(false);
            int messagesPerClient = this.MessageCount / this.ConcurrentClients;
            for (var i = 0; i < messagesPerClient; i++)
            {
                var msgId = Guid.NewGuid();
                this._sentMessages.TryAdd(msgId, new MessageData
                {
                    Handle = latencyHandle,
                    Timestamp = DateTime.UtcNow
                });

                clientHandler.SendCommand(new BenchmarkCommand
                {
                    MessageId = msgId,
                    Payload = GeneratePayload(this.MessageSize),
                    UseReliableMode = networkMode == NetworkMode.Reliable,
                    Timestamp = DateTime.UtcNow
                }, NetworkUserId.Everyone, networkMode);
            }

            latencyHandle.Wait();
        })));
    }

    private static string GeneratePayload(int size)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var payload = new char[size];
        Random random = new();

        for (var i = 0; i < size; i++)
            payload[i] = chars[random.Next(chars.Length)];

        return new string(payload);
    }
}

internal record struct MessageData
{
    public required ManualResetEventSlim Handle { get; init; }
    public required DateTime Timestamp { get; init; }
}