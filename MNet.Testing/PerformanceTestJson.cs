
using Microsoft.Extensions.Logging;
using MNet.Tcp.Options;
using MNet.Tcp;

namespace MNet.Testing;

internal static class PerformanceTestJson {

    public static void Run(ILogger debugLogger, ) {

        long countPerSecond = 0;
        var server = new TcpServer(new TcpServerOptions() {
            Address = "127.0.0.1",
            Port = 30300,
        });



    }

}
