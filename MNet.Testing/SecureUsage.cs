using MNet.Tcp.Handshakers;
using MNet.Tcp.Options;
using MNet.Tcp.Serializers;
using MNet.Tcp;
using Microsoft.Extensions.Logging;
using MNet.Helpers;
using Serilog.Core;
using System.Security.Cryptography.X509Certificates;

namespace MNet.Testing;

internal class SecureUsage {

    public static void Run(ILogger debugLogger) {

        X509Certificate2? cert = null;
        try {

            X509Certificate2Collection collection = new X509Certificate2Collection();
            collection.Import("cert.pfx",
                "123", X509KeyStorageFlags.PersistKeySet);

            cert = collection[0];

        } catch (Exception er) {

            debugLogger.LogError("{Error}", er);

        }

        var server = new TcpServer(new TcpServerOptions() {
            Address = "127.0.0.1",
            Port = 43434,
            IsSecure = true,
            Certificate = cert,
            Logger = debugLogger, // ILogger of your liking, default is just console one
            Serializer = new TcpJsonSerializer(), // by default TcpJsonSerializer, you can implement your own serializers with ITcpSerializer
            Handshaker = new TcpServerHandshaker(), // by default no handshaking, if you need handshaking implement a ITcpServerHandshaker
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

        var client = new TcpClient(new TcpClientOptions() {
            Address = "127.0.0.1",
            Port = 43434,
            IsSecure = true,
            Host = "localhost",
            Logger = debugLogger, // ILogger of your liking, default is just console one
            Serializer = new TcpJsonSerializer(), // by default TcpJsonSerializer, you can implement your own serializers with ITcpSerializer
            Handshaker = new TcpClientHandshaker(), // by default no handshaking, if you need handshaking implement a ITcpClientHandshaker
        });

        client.OnConnect += () => {

            client.Send("test-class", new Test() { A = "WHOOP" });

            Memory<byte> random = new byte[1024 * 1024 * 30];
            var span = random.Span;
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