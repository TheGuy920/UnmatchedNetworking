using UnmatchedNetworking.Networking;

namespace ServerTest;

[NetworkCommand("d2f4aec8-1234-4b6d-9876-f1d2d8201653")]
internal partial class BenchmarkCommand
{
    public Guid MessageId { get; init; }
    public Guid ClientId { get; init; }
    public string Payload { get; init; } = string.Empty;
    public bool IsEchoRequest { get; init; }
    public bool IsThroughputTest { get; init; }
    public bool UseReliableMode { get; init; }
    public DateTime Timestamp { get; init; }
}