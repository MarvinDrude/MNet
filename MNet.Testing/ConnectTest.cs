
using MNet.Tcp.Handshakers;
using MNet.Tcp.Options;
using MNet.Tcp.Serializers;
using MNet.Tcp;
using Microsoft.Extensions.Logging;

namespace MNet.Testing;

internal class ConnectTest {

    public static void Run(ILogger debugLogger, ILogger noneLogger) {

        var server = new TcpServer(new TcpServerOptions() {
            Address = "127.0.0.1",
            Port = 43434,
            Logger = noneLogger
            //Logger = debugLogger,
        });

        server.OnConnect += async (connection) => {

            debugLogger.LogInformation("Connected {Count}", server.ConnectionCount);
            await Task.Delay(300);

            connection.Disconnect();

        };

        server.OnDisconnect += (connection) => {

            debugLogger.LogInformation("Disconnected {Count}", server.ConnectionCount);

        };

        server.Start();

        for (int e = 0; e < 150; e++) {

            var client = new TcpClient(new TcpClientOptions() {
                Address = "127.0.0.1",
                Port = 43434,
                Logger = noneLogger
                //Logger = debugLogger,
            });

            client.OnConnect += async () => {

                client.Send("test", new Memory<byte>([2]));
                debugLogger.LogInformation("Client connect");

            };

            client.OnDisconnect += async () => {

                debugLogger.LogInformation("Client disconnect");

            };

            client.Connect();

        }

    }

}
