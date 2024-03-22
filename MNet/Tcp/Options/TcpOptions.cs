
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;

namespace MNet.Tcp.Options;

public class TcpOptions {

    private static readonly Logger DefaultSerilogLogger;

    private static readonly ILoggerFactory DefaultLoggerFactory;

    static TcpOptions() {

        DefaultSerilogLogger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .MinimumLevel.Information()
            .CreateLogger();

        DefaultLoggerFactory = new LoggerFactory([], new LoggerFilterOptions() { 
                MinLevel = LogLevel.Information
            })
            .AddSerilog(DefaultSerilogLogger);

    }

    public required string Address { get; init; }

    public required ushort Port { get; init; }

    public bool IsSecure { get; init; } = false;

    public int MaxHandshakeSizeBytes { get; init; } = 1024 * 2;

    public SocketConnectionOptions SocketConnectionOptions { get; init; } = new();

    public StreamConnectionOptions StreamConnectionOptions { get; init; } = new();

    /// <summary>
    /// Serializer for object sending, default is the tcp json serializer
    /// </summary>
    public ITcpSerializer Serializer { get; init; } = new TcpJsonSerializer();

    /// <summary>
    /// Frame factory, default is simple | len - id - len - body frame |
    /// </summary>
    public ITcpFrameFactory FrameFactory { get; init; } = new TcpFrameFactory();

    /// <summary>
    /// Optionally set your own logger or get the default one
    /// </summary>
    public Microsoft.Extensions.Logging.ILogger Logger { get; init; } = DefaultLoggerFactory.CreateLogger("TcpLogging");

    /// <summary>
    /// Only used for internal testing, shouldn't be set in production!
    /// </summary>
    public TcpUnderlyingConnectionType ConnectionType { get; init; } = TcpUnderlyingConnectionType.Unset;

}

public enum TcpUnderlyingConnectionType : byte {

    Unset = 0,
    NetworkStream = 1,
    SslStream = 2,
    FastSocket = 3

}