
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

//BasicUsage.Run(debugLogger);
//await PerformanceTestJson.Run(debugLogger, TcpUnderlyingConnectionType.NetworkStream);
await PerformanceTestBinary.Run(debugLogger, TcpUnderlyingConnectionType.NetworkStream);

while (true) {
    await Task.Delay(100);
}
