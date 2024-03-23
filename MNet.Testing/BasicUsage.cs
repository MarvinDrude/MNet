
using Microsoft.Extensions.Logging;
using MNet.Tcp.Options;
using MNet.Tcp;

namespace MNet.Testing;

internal class BasicUsage {

    public static void Run(ILogger debugLogger) {

        var server = new TcpServer(new TcpServerOptions() {
            Address = "127.0.0.1",
            Port = 43434,
            Logger = debugLogger,
            ConnectionType = TcpUnderlyingConnectionType.NetworkStream
        });

        server.On<ReadOnlyMemory<byte>>("test-bytes", (buffer, connection) => {

            Console.WriteLine("Length: " + buffer.Length);

        });

        server.On<Test>("test-bytes", (message, connection) => {

            if (message == null) return;

            Console.WriteLine("A: " + message.A);

        });

        server.Start();
        server.Stop();
        server.Start();

        var client = new TcpClient(new TcpClientOptions() {
            Address = "127.0.0.1",
            Port = 43434,
            Logger = debugLogger,
            ConnectionType = TcpUnderlyingConnectionType.NetworkStream
        });

        client.OnConnect += () => {

            Console.WriteLine("Connected client.");

        };

        client.Connect();

    }

}

class Test {

    public required string A { get; set; }

}