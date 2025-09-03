using System.Threading.Tasks;

namespace UnmatchedNetworking.Internal;

internal static class Extensions
{
    public static void Wait(this ValueTask task)
        => task.GetAwaiter().GetResult();
}