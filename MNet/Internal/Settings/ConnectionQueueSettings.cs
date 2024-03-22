
namespace MNet.Internal.Settings;

internal class ConnectionQueueSettings {

    public PipeOptions InputOptions { get; init; } = default!;

    public PipeOptions OutputOptions { get; init; } = default!;

    public MemoryPool<byte> MemoryPool { get; init; } = default!;

}
