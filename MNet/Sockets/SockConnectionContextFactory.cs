
namespace MNet.Sockets;

// taken from kestrel internal sockets
/// <summary>
/// A factory for socket based connections contexts.
/// </summary>
internal sealed class SockConnectionContextFactory : IDisposable {

    private readonly SockConnectionFactoryOptions _options;
    private readonly int _settingsCount;
    private readonly QueueSettings[] _settings;

    // long to prevent overflow
    private long _settingsIndex;

    /// <summary>
    /// Creates the <see cref="SocketConnectionContextFactory"/>.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="logger">The logger.</param>
    public SockConnectionContextFactory(SockConnectionFactoryOptions options) {

        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _settingsCount = _options.IOQueueCount;

        var maxReadBufferSize = _options.MaxReadBufferSize ?? 0;
        var maxWriteBufferSize = _options.MaxWriteBufferSize ?? 0;
        var applicationScheduler = options.UnsafePreferInlineScheduling ? PipeScheduler.Inline : PipeScheduler.ThreadPool;

        if (_settingsCount > 0) {
            _settings = new QueueSettings[_settingsCount];

            for (var i = 0; i < _settingsCount; i++) {
                var memoryPool = _options.MemoryPoolFactory();
                var transportScheduler = options.UnsafePreferInlineScheduling ? PipeScheduler.Inline : new IOQueue();

                _settings[i] = new QueueSettings() {
                    Scheduler = transportScheduler,
                    InputOptions = new PipeOptions(memoryPool, applicationScheduler, transportScheduler, maxReadBufferSize, maxReadBufferSize / 2, useSynchronizationContext: false),
                    OutputOptions = new PipeOptions(memoryPool, transportScheduler, applicationScheduler, maxWriteBufferSize, maxWriteBufferSize / 2, useSynchronizationContext: false),
                    SocketSenderPool = new SockSenderPool(PipeScheduler.Inline),
                    MemoryPool = memoryPool,
                };
            }
        } else {
            var memoryPool = _options.MemoryPoolFactory();
            var transportScheduler = options.UnsafePreferInlineScheduling ? PipeScheduler.Inline : PipeScheduler.ThreadPool;

            _settings = new QueueSettings[]
            {
                new QueueSettings()
                {
                    Scheduler = transportScheduler,
                    InputOptions = new PipeOptions(memoryPool, applicationScheduler, transportScheduler, maxReadBufferSize, maxReadBufferSize / 2, useSynchronizationContext: false),
                    OutputOptions = new PipeOptions(memoryPool, transportScheduler, applicationScheduler, maxWriteBufferSize, maxWriteBufferSize / 2, useSynchronizationContext: false),
                    SocketSenderPool = new SockSenderPool(PipeScheduler.Inline),
                    MemoryPool = memoryPool,
                }
            };
            _settingsCount = 1;
        }
    }

    /// <summary>
    /// Create a <see cref="ConnectionContext"/> for a socket.
    /// </summary>
    /// <param name="socket">The socket for the connection.</param>
    /// <returns></returns>
    public SockConnection Create(Socket socket) {

        var setting = _settings[Interlocked.Increment(ref _settingsIndex) % _settingsCount];

        var connection = new SockConnection(socket,
            setting.MemoryPool,
            setting.SocketSenderPool.Scheduler,
            setting.SocketSenderPool,
            setting.InputOptions,
            setting.OutputOptions,
            waitForData: _options.WaitForDataBeforeAllocatingBuffer,
            finOnError: _options.FinOnError);

        connection.Start();
        return connection;

    }

    /// <inheritdoc />
    public void Dispose() {

        // Dispose any pooled senders and memory pools
        foreach (var setting in _settings) {

            setting.SocketSenderPool.Dispose();
            setting.MemoryPool.Dispose();

        }

    }

    private sealed class QueueSettings {

        public PipeScheduler Scheduler { get; init; } = default!;

        public PipeOptions InputOptions { get; init; } = default!;

        public PipeOptions OutputOptions { get; init; } = default!;

        public SockSenderPool SocketSenderPool { get; init; } = default!;

        public MemoryPool<byte> MemoryPool { get; init; } = default!;

    }

}

/// <summary>
/// Encapsulates all information about an individual connection.
/// </summary>
public abstract class ConnectionContext : BaseConnectionContext, IAsyncDisposable {

    /// <summary>
    /// Gets or sets the <see cref="IDuplexPipe"/> that can be used to read or write data on this connection.
    /// </summary>
    public abstract IDuplexPipe Transport { get; set; }

    /// <summary>
    /// Aborts the underlying connection.
    /// </summary>
    /// <param name="abortReason">A <see cref="ConnectionAbortedException"/> describing the reason the connection is being terminated.</param>
    public override void Abort(Exception abortReason) {
        
    }

    /// <summary>
    /// Aborts the underlying connection.
    /// </summary>
    public override void Abort() => Abort(new Exception("The connection was aborted by the application via ConnectionContext.Abort()."));
}

/// <summary>
/// Represents the context for a connection.
/// </summary>
public abstract class BaseConnectionContext : IAsyncDisposable {
    /// <summary>
    /// Gets or sets a unique identifier to represent this connection in trace logs.
    /// </summary>
    public abstract string ConnectionId { get; set; }

    /// <summary>
    /// Gets or sets a key/value collection that can be used to share data within the scope of this connection.
    /// </summary>
    public abstract IDictionary<object, object?> Items { get; set; }

    /// <summary>
    /// Triggered when the client connection is closed.
    /// </summary>
    public virtual CancellationToken ConnectionClosed { get; set; }

    /// <summary>
    /// Gets or sets the local endpoint for this connection.
    /// </summary>
    public virtual EndPoint? LocalEndPoint { get; set; }

    /// <summary>
    /// Gets or sets the remote endpoint for this connection.
    /// </summary>
    public virtual EndPoint? RemoteEndPoint { get; set; }

    /// <summary>
    /// Aborts the underlying connection.
    /// </summary>
    public abstract void Abort();

    /// <summary>
    /// Aborts the underlying connection.
    /// </summary>
    /// <param name="abortReason">A <see cref="ConnectionAbortedException"/> describing the reason the connection is being terminated.</param>
    public abstract void Abort(Exception abortReason);

    /// <summary>
    /// Releases resources for the underlying connection.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that completes when resources have been released.</returns>
    public virtual ValueTask DisposeAsync() {
        return default;
    }
}

/// <summary>
/// Options for <see cref="SocketConnectionContextFactory"/>.
/// </summary>
public class SockConnectionFactoryOptions {

    /// <summary>
    /// Create a new instance.
    /// </summary>
    public SockConnectionFactoryOptions() { }

    internal SockConnectionFactoryOptions(SocketTransportOptions transportOptions) {
        IOQueueCount = transportOptions.IOQueueCount;
        WaitForDataBeforeAllocatingBuffer = transportOptions.WaitForDataBeforeAllocatingBuffer;
        MaxReadBufferSize = transportOptions.MaxReadBufferSize;
        MaxWriteBufferSize = transportOptions.MaxWriteBufferSize;
        UnsafePreferInlineScheduling = transportOptions.UnsafePreferInlineScheduling;
        MemoryPoolFactory = transportOptions.MemoryPoolFactory;
        FinOnError = transportOptions.FinOnError;
    }

    // Opt-out flag for back compat. Remove in 9.0 (or make public).
    internal bool FinOnError { get; set; }

    /// <summary>
    /// The number of I/O queues used to process requests. Set to 0 to directly schedule I/O to the ThreadPool.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="Environment.ProcessorCount" /> rounded down and clamped between 1 and 16.
    /// </remarks>
    public int IOQueueCount { get; set; } = Math.Min(Environment.ProcessorCount, 16);

    /// <summary>
    /// Wait until there is data available to allocate a buffer. Setting this to false can increase throughput at the cost of increased memory usage.
    /// </summary>
    /// <remarks>
    /// Defaults to true.
    /// </remarks>
    public bool WaitForDataBeforeAllocatingBuffer { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum unconsumed incoming bytes the transport will buffer.
    /// </summary>
    public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum outgoing bytes the transport will buffer before applying write backpressure.
    /// </summary>
    public long? MaxWriteBufferSize { get; set; } = 64 * 1024;

    /// <summary>
    /// Inline application and transport continuations instead of dispatching to the threadpool.
    /// </summary>
    /// <remarks>
    /// This will run application code on the IO thread which is why this is unsafe.
    /// It is recommended to set the DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS environment variable to '1' when using this setting to also inline the completions
    /// at the runtime layer as well.
    /// This setting can make performance worse if there is expensive work that will end up holding onto the IO thread for longer than needed.
    /// Test to make sure this setting helps performance.
    /// </remarks>
    public bool UnsafePreferInlineScheduling { get; set; }

    internal Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = PinnedBlockMemoryPoolFactory.Create;

}

/// <summary>
/// Options for socket based transports.
/// </summary>
public class SocketTransportOptions {

    private const string FinOnErrorSwitch = "Microsoft.AspNetCore.Server.Kestrel.FinOnError";
    private static readonly bool _finOnError;

    static SocketTransportOptions() {
        AppContext.TryGetSwitch(FinOnErrorSwitch, out _finOnError);
    }

    // Opt-out flag for back compat. Remove in 9.0 (or make public).
    internal bool FinOnError { get; set; } = _finOnError;

    /// <summary>
    /// The number of I/O queues used to process requests. Set to 0 to directly schedule I/O to the ThreadPool.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="Environment.ProcessorCount" /> rounded down and clamped between 1 and 16.
    /// </remarks>
    public int IOQueueCount { get; set; } = Math.Min(Environment.ProcessorCount, 16);

    /// <summary>
    /// Wait until there is data available to allocate a buffer. Setting this to false can increase throughput at the cost of increased memory usage.
    /// </summary>
    /// <remarks>
    /// Defaults to true.
    /// </remarks>
    public bool WaitForDataBeforeAllocatingBuffer { get; set; } = true;

    /// <summary>
    /// Set to false to enable Nagle's algorithm for all connections.
    /// </summary>
    /// <remarks>
    /// Defaults to true.
    /// </remarks>
    public bool NoDelay { get; set; } = true;

    /// <summary>
    /// The maximum length of the pending connection queue.
    /// </summary>
    /// <remarks>
    /// Defaults to 512 pending connections.
    /// </remarks>
    public int Backlog { get; set; } = 512;

    /// <summary>
    /// Gets or sets the maximum unconsumed incoming bytes the transport will buffer.
    /// <para>
    /// A value of <see langword="null"/> or 0 disables backpressure entirely allowing unlimited buffering.
    /// Unlimited server buffering is a security risk given untrusted clients.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Defaults to 1 MiB.
    /// </remarks>
    public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum outgoing bytes the transport will buffer before applying write backpressure.
    /// <para>
    /// A value of <see langword="null"/> or 0 disables backpressure entirely allowing unlimited buffering.
    /// Unlimited server buffering is a security risk given untrusted clients.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Defaults to 64 KiB.
    /// </remarks>
    public long? MaxWriteBufferSize { get; set; } = 64 * 1024;

    /// <summary>
    /// Inline application and transport continuations instead of dispatching to the threadpool.
    /// </summary>
    /// <remarks>
    /// This will run application code on the IO thread which is why this is unsafe.
    /// It is recommended to set the DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS environment variable to '1' when using this setting to also inline the completions
    /// at the runtime layer as well.
    /// This setting can make performance worse if there is expensive work that will end up holding onto the IO thread for longer than needed.
    /// Test to make sure this setting helps performance.
    /// </remarks>
    /// <remarks>
    /// Defaults to false.
    /// </remarks>
    public bool UnsafePreferInlineScheduling { get; set; }

    /// <summary>
    /// A function used to create a new <see cref="Socket"/> to listen with. If
    /// not set, <see cref="CreateDefaultBoundListenSocket" /> is used.
    /// </summary>
    /// <remarks>
    /// Implementors are expected to call <see cref="Socket.Bind"/> on the
    /// <see cref="Socket"/>. Please note that <see cref="CreateDefaultBoundListenSocket"/>
    /// calls <see cref="Socket.Bind"/> as part of its implementation, so implementors
    /// using this method do not need to call it again.
    /// </remarks>
    /// <remarks>
    /// Defaults to <see cref="CreateDefaultBoundListenSocket"/>.
    /// </remarks>
    public Func<EndPoint, Socket> CreateBoundListenSocket { get; set; } = CreateDefaultBoundListenSocket;

    /// <summary>
    /// Creates a default instance of <see cref="Socket"/> for the given <see cref="EndPoint"/>
    /// that can be used by a connection listener to listen for inbound requests. <see cref="Socket.Bind"/>
    /// is called by this method.
    /// </summary>
    /// <param name="endpoint">
    /// An <see cref="EndPoint"/>.
    /// </param>
    /// <returns>
    /// A <see cref="Socket"/> instance.
    /// </returns>
    public static Socket CreateDefaultBoundListenSocket(EndPoint endpoint) {
        Socket listenSocket;
        switch (endpoint) {
            case FileHandleEndPoint fileHandle:
                // We're passing "ownsHandle: false" to avoid side-effects on the
                // handle when disposing the socket.
                //
                // When the non-owning SafeSocketHandle gets disposed (on .NET 7+),
                // on-going async operations are aborted.
                listenSocket = new Socket(
                    new SafeSocketHandle((IntPtr)fileHandle.FileHandle, ownsHandle: false)
                );
                break;
            case UnixDomainSocketEndPoint unix:
                listenSocket = new Socket(unix.AddressFamily, SocketType.Stream, ProtocolType.Unspecified);
                break;
            case IPEndPoint ip:
                listenSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Kestrel expects IPv6Any to bind to both IPv6 and IPv4
                if (ip.Address.Equals(IPAddress.IPv6Any)) {
                    listenSocket.DualMode = true;
                }

                break;
            default:
                listenSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                break;
        }

        // we only call Bind on sockets that were _not_ created
        // using a file handle; the handle is already bound
        // to an underlying socket so doing it again causes the
        // underlying PAL call to throw
        if (!(endpoint is FileHandleEndPoint)) {
            listenSocket.Bind(endpoint);
        }

        return listenSocket;
    }

    internal Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = PinnedBlockMemoryPoolFactory.Create;
}

/// <summary>
/// An endpoint backed by an OS file handle.
/// </summary>
public class FileHandleEndPoint : EndPoint {
    /// <summary>
    /// Initializes a new instance of <see cref="FileHandleEndPoint"/>.
    /// </summary>
    /// <param name="fileHandle">The file handle.</param>
    /// <param name="fileHandleType">The file handle type.</param>
    public FileHandleEndPoint(ulong fileHandle, FileHandleType fileHandleType) {
        FileHandle = fileHandle;
        FileHandleType = fileHandleType;

        switch (fileHandleType) {
            case FileHandleType.Auto:
            case FileHandleType.Tcp:
            case FileHandleType.Pipe:
                break;
            default:
                throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Gets the file handle.
    /// </summary>
    public ulong FileHandle { get; }

    /// <summary>
    /// Gets the file handle type.
    /// </summary>
    public FileHandleType FileHandleType { get; }
}

/// <summary>
/// Enumerates the <see cref="FileHandleEndPoint"/> types.
/// </summary>
public enum FileHandleType {
    /// <summary>
    /// This API is unused and no longer supported.
    /// </summary>
    Auto,

    /// <summary>
    /// This API is unused and no longer supported.
    /// </summary>
    Tcp,

    /// <summary>
    /// This API is unused and no longer supported.
    /// </summary>
    Pipe
}

internal static class PinnedBlockMemoryPoolFactory {
    public static MemoryPool<byte> Create() {
        return CreatePinnedBlockMemoryPool();
    }

    public static MemoryPool<byte> CreatePinnedBlockMemoryPool() {
        return new PinnedBlockMemoryPool();
    }
}