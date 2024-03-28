
using MNet.Tcp.Handshakers;
using MNet.Tcp.Options;
using MNet.Tcp.Serializers;
using MNet.Tcp;
using Microsoft.Extensions.Logging;
using MNet.Helpers;
using System.Buffers.Binary;
using System.Buffers;

namespace MNet.Testing;

internal class RandomTest {

    public static async Task Run(ILogger debugLogger) {

        long count = 0;

        var server = new TcpServer(new TcpServerOptions() {
            Address = "127.0.0.1",
            Port = 43434,
            Logger = debugLogger,
        });

        server.On<ReadOnlyMemory<byte>>("test-bytes", (buffer, connection) => {

            Interlocked.Increment(ref count);

            var first = buffer.Span[..(buffer.Length / 2)];
            var second = buffer.Span[(buffer.Length / 2)..];

            if(!first.SequenceEqual(second)) {
                Console.WriteLine("MISMATCH BINARY");
            }

        });

        server.On<Test>("test-class", (message, connection) => {

            if (message == null) return;
            Interlocked.Increment(ref count);

            if(message.A != message.B) {
                Console.WriteLine("MISMATCH");
            }

        });

        server.OnConnect += (connection) => {

            debugLogger.LogInformation("Connected {Count}", server.ConnectionCount);

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
            Logger = debugLogger,
        });

        client.OnConnect += () => {

            for (int e = 0; e < 24; e++) {

                var _ = RunMessages(client);

            }

        };

        client.OnDisconnect += () => {

            Console.WriteLine("Client disconnect");

        };

        client.Connect();

        while(true) {

            await Task.Delay(1000);
            Console.WriteLine("OP/s: " + count);

            Interlocked.Exchange(ref count, 0);

        }

    }

    public static async Task RunMessages(TcpClient client) {

        void send() {

            var length = 20;
            using var owner = MemoryPool<byte>.Shared.Rent(length);

            var arr = owner.Memory.Span.Slice(0, length);
            RandomUtils.RandomBytes(ref arr);

            arr[..(arr.Length / 2)].CopyTo(arr[(arr.Length / 2)..]);

            client.Send("test-bytes", new Memory<byte>(arr.ToArray()));

        }

        while(true) {

            for(int e = 0; e < 30; e++) {

                var str = RandomUtils.RandomString(16);

                client.Send("test-class", new Test() { 
                    A = str,
                    B = str
                });

                send();

            }

            await Task.Delay(RandomUtils.Next(1, 5));

        }

    }

}
