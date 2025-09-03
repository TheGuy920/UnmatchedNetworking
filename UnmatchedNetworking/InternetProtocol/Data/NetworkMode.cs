namespace UnmatchedNetworking.InternetProtocol.Data;

public enum NetworkMode : byte
{
    /// <summary>
    ///     Send the packet using Default.
    /// </summary>
    NA = 0,

    /// <summary>
    ///     Send the packet using TCP.
    /// </summary>
    Reliable = 1,

    /// <summary>
    ///     Send the packet using UDP.
    /// </summary>
    Unreliable = 2
}