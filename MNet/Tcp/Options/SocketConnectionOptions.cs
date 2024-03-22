

namespace MNet.Tcp.Options;

public sealed class SocketConnectionOptions : ConnectionOptions {

    internal override ConnectionQueueSettings CreateQueueSettings() {

        var memoryPool = new PinnedBlockMemoryPool();
        var scheduler = new IOQueue();

        var maxReadBufferSize = MaxReadBufferSize ?? 0;
        var maxWriteBufferSize = MaxWriteBufferSize ?? 0;

        return new SocketConnectionQueueSettings() {

            MemoryPool = memoryPool,
            InputOptions = new PipeOptions(
                memoryPool, PipeScheduler.ThreadPool, PipeScheduler.ThreadPool,
                maxReadBufferSize, maxReadBufferSize / 2,
                useSynchronizationContext: false),
            OutputOptions = new PipeOptions(
                memoryPool, PipeScheduler.ThreadPool, PipeScheduler.ThreadPool,
                maxWriteBufferSize, maxWriteBufferSize / 2,
                useSynchronizationContext: false),
            Scheduler = scheduler,
            SocketSenderPool = new SocketSenderPool(PipeScheduler.Inline)

        };

    }

}
