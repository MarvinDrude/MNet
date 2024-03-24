
using Microsoft.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog;
using MNet.Testing;
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

//SecureUsage.Run(debugLogger);

//BasicUsage.Run(debugLogger);

WsBasicUsage.Run(debugLogger);

//await PerformanceTestJson.Run(debugLogger, TcpUnderlyingConnectionType.FastSocket);

//await PerformanceTestBinary.Run(debugLogger, TcpUnderlyingConnectionType.FastSocket);



while (true) {
    await Task.Delay(100);
}
