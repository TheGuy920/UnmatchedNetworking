using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnmatchedNetworking.InternetProtocol;
using UnmatchedNetworking.InternetProtocol.Data;

namespace UnmatchedNetworking;

[PublicAPI]
public class NetworkingService : INetworkingService
{
    private CancellationTokenSource _cancellationTokenSource = new();
    private ConcurrentDictionary<Guid, NetworkingCommandHandler> _registeredCommands = [];
    private ConcurrentDictionary<Type, Guid> _registeredCommandsGuidTypeMap = [];
    private INetworkingBackend _backend;
    private TaskRunner _taskRunner = Task.Run;
    private ThreadSleeper _threadSleeper = Thread.Sleep;
    private object _eventProcessRateLock = new();

    private NetworkingService(INetworkingBackend backend)
    {
        this._backend = backend;
        this._backend.PacketReceived += this.OnPacketReceived;
    }

    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T RegisterCommandHandler<T>() where T : NetworkingCommandHandler
        => this.RegisterCommandHandler<T>(null);

    /// <summary>
    /// </summary>
    /// <param name="args"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T RegisterCommandHandler<T>(params object[]? args) where T : NetworkingCommandHandler
        => this.GetOrAddCommand<T>(args);

    /// <summary>
    /// </summary>
    /// <param name="taskRunner"></param>
    public void SetTaskRunner(TaskRunner taskRunner)
        => this._taskRunner = taskRunner;

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public void Run()
    {
        try
        {
            bool res = this._backend.Connect();
            if (!res)
                throw new Exception("Failed to connect to backend");

            object backendLock = new();
            this._cancellationTokenSource.Token.Register(() =>
            {
                // Wait for the backend to finish processing events
                lock (backendLock) ;
            });

            lock (backendLock)
            {
                TimeSpan processRate;
                lock (this._eventProcessRateLock)
                    processRate = this.EventProcessRate;

                // Console.WriteLine("Running network service: " + this._backend.GetType().GenericTypeArguments[0].Name);
                while (!this._cancellationTokenSource.IsCancellationRequested)
                {
                    this._backend.ProcessEvents();
                    this._threadSleeper(processRate);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            this._backend.Disconnect();
        }
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public Task RunAsync()
        => this._taskRunner(this.Run, this._cancellationTokenSource.Token);

    /// <summary>
    /// </summary>
    /// <param name="timeout"></param>
    public bool Stop(TimeSpan? timeout = null)
    {
        if (timeout is not null)
        {
            this._cancellationTokenSource.CancelAfter(TimeSpan.Zero);
            return this._cancellationTokenSource.Token.WaitHandle.WaitOne(timeout.Value);
        }

        this._cancellationTokenSource.CancelAfter(TimeSpan.Zero);
        return this._cancellationTokenSource.Token.WaitHandle.WaitOne();
    }

    /// <summary>
    /// </summary>
    /// <param name="eventProcessRate"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void SetEventProcessRate(TimeSpan eventProcessRate)
    {
        if (eventProcessRate < TimeSpan.FromTicks(1))
            throw new ArgumentOutOfRangeException(nameof(eventProcessRate), "Event process rate must be greater than 0");

        lock (this._eventProcessRateLock)
            this.EventProcessRate = eventProcessRate;
    }

    /// <summary>
    /// </summary>
    public TimeSpan EventProcessRate { get; private set; } = new(1_000);

    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static NetworkingService Create<T>() where T : class, INetworkingBackend, new()
        => new(new T());

    /// <summary>
    /// </summary>
    /// <param name="args"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static NetworkingService Create<T>(params object[]? args) where T : class, INetworkingBackend
    {
        var backend = (INetworkingBackend)Activator.CreateInstance(typeof(T), args);
        if (backend == null)
            throw new ArgumentNullException(nameof(backend));

        return new NetworkingService(backend);
    }

    public void WaitUntilConnected()
    {
        TimeSpan rate = this.EventProcessRate;
        while (!this._backend.IsConnected)
            this._threadSleeper(rate);
    }

    private void OnPacketReceived(NetworkUserId sender, RawPacket rawPacket)
    {
        var commandId = rawPacket.Read<Guid>();
        if (!this._registeredCommands.TryGetValue(commandId, out NetworkingCommandHandler? command))
            return;

        command.Process(sender, rawPacket);
    }

    private T CreateCommand<T>(object[]? args) where T : NetworkingCommandHandler
        => (T)Activator.CreateInstance(typeof(T), args);

    private T GetOrAddCommand<T>(object[]? args) where T : NetworkingCommandHandler
    {
        if (this._registeredCommandsGuidTypeMap.TryGetValue(typeof(T), out Guid commandId)
            && this._registeredCommands.TryGetValue(commandId, out NetworkingCommandHandler? command))
            return (T)command;

        var cmd = this.CreateCommand<T>(null);
        cmd.OnSendPacket += this._backend.SendPacket;
        this._registeredCommands.TryAdd(cmd.TypeId, cmd);
        this._registeredCommandsGuidTypeMap.TryAdd(typeof(T), cmd.TypeId);
        return cmd;
    }
}