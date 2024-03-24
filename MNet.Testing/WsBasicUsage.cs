using Microsoft.Extensions.Logging;
using MNet.Ws;
using MNet.Ws.Options;
using MNet.Helpers;

namespace MNet.Testing;

internal class WsBasicUsage {

    public static void Run(ILogger debugLogger) {

        // ws implementation not ready yet

        var server = new WsServer(new WsServerOptions() {
            Address = "127.0.0.1",
            Port = 43434,
            Logger = debugLogger,
        });

        server.On<ReadOnlyMemory<byte>>("test-bytes", (buffer, connection) => {

            Console.WriteLine("Length: " + buffer.Length);

        });

        server.On<Test>("test-class", (message, connection) => {

            if (message == null) return;

            Console.WriteLine("A: " + message.A);

        });

        server.OnConnect += (connection) => {

            debugLogger.LogInformation("Connected {Count}", server.ConnectionCount);

            connection.Send("test-class", new Test() { A = "WHOOP-Server" });
            connection.Send("test-bytes", new Memory<byte>([0, 2, 3, 5]));

        };

        server.OnDisconnect += (connection) => {

            debugLogger.LogInformation("Disconnected {Count}", server.ConnectionCount);

        };

        server.Start();
        server.Stop();
        server.Start();

        var client = new WsClient(new WsClientOptions() {
            Address = "127.0.0.1",
            Port = 43434,
            Logger = debugLogger,
        });

        client.OnConnect += () => {

            client.Send("test-class", new Test() { A = "WHOOP" });

            Memory<byte> random = new byte[1024 * 1024 * 30];
            var span = random.Span;
            RandomUtils.RandomBytes(ref span);

            client.Send("test-bytes", random);

            random = new byte[33000];
            span = random.Span;
            RandomUtils.RandomBytes(ref span);

            client.Send("test-bytes", random);

            random = new byte[2];
            span = random.Span;
            RandomUtils.RandomBytes(ref span);

            client.Send("test-bytes", random);

        };

        client.OnDisconnect += () => {

            Console.WriteLine("Client connect");

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
