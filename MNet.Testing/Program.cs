
using Microsoft.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog;
using MNet.Testing;

var debugSerilogLogger = new LoggerConfiguration()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .MinimumLevel.Debug()
    .CreateLogger();
var debugLoggerFactory = new LoggerFactory([], new LoggerFilterOptions() {
    MinLevel = LogLevel.Debug
})
.AddSerilog(debugSerilogLogger);
var debugLogger = debugLoggerFactory.CreateLogger("TcpLogging");

BasicUsage.Run(debugLogger);

while (true) {
    await Task.Delay(100);
}
