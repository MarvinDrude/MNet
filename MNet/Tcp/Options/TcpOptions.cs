
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

    public required string Address { get; set; }

    public required ushort Port { get; set; }

    public bool IsSecure { get; set; } = false;

    public int MaxHandshakeSizeBytes { get; set; } = 1024 * 2;

    public SocketConnectionOptions SocketConnectionOptions { get; set; } = new();

    public StreamConnectionOptions StreamConnectionOptions { get; set; } = new();

    /// <summary>
    /// Serializer for object sending, default is the tcp json serializer
    /// </summary>
    public ITcpSerializer Serializer { get; set; } = new TcpJsonSerializer();

    /// <summary>
    /// Frame factory, default is simple | len - id - len - body frame |
    /// </summary>
    public ITcpFrameFactory FrameFactory { get; set; } = new TcpFrameFactory();

    /// <summary>
    /// Optionally set your own logger or get the default one
    /// </summary>
    public Microsoft.Extensions.Logging.ILogger? Logger { get; set; } = DefaultLoggerFactory.CreateLogger("TcpLogging");

    /// <summary>
    /// Only used for internal testing
    /// </summary>
    internal TcpUnderlyingConnectionType ConnectionType { get; set; } = TcpUnderlyingConnectionType.Unset;

}

public enum TcpUnderlyingConnectionType : byte {

    Unset = 0,
    NetworkStream = 1,
    SslStream = 2,
    FastSocket = 3

}