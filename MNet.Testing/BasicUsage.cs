
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

        server.On<Test>("test-class", (message, connection) => {

            if (message == null) return;

            Console.WriteLine("A: " + message.A);

        });

        server.OnConnect += (connection) => {

            connection.Send("test-class", new Test() { A = "WHOOP-Server" });
            connection.Send("test-bytes", new Memory<byte>([0, 2, 3, 5]));

        };

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

            client.Send("test-class", new Test() { A = "WHOOP" });
            client.Send("test-bytes", new Memory<byte>([0, 2, 3]));

        };

        client.On<ReadOnlyMemory<byte>>("test-bytes", (buffer) => {

            Console.WriteLine("Length: " + buffer.Length);

        });

        client.On<Test>("test-class", (message) => {

            if (message == null) return;

            Console.WriteLine("A: " + message.A);

        });

        client.Connect();

    }

}

class Test {

    public required string A { get; set; }

}