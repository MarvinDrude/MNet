
using Microsoft.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog;
using MNet.Tcp;
using MNet.Tcp.Options;

var debugSerilogLogger = new LoggerConfiguration()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .MinimumLevel.Debug()
    .CreateLogger();

var debugLoggerFactory = new LoggerFactory([], new LoggerFilterOptions() {
    MinLevel = LogLevel.Debug
})
.AddSerilog(debugSerilogLogger);

var debugLogger = debugLoggerFactory.CreateLogger("TcpLogging");

var server = new TcpServer(new TcpServerOptions() {
    Address = "127.0.0.1",
    Port = 43434,
    Logger = debugLogger,
    ConnectionType = TcpUnderlyingConnectionType.NetworkStream
});



while(true) {
    await Task.Delay(100);
}
