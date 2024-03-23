
using Microsoft.Extensions.Logging;
using MNet.Tcp.Options;
using MNet.Tcp;
using MNet.Helpers;

namespace MNet.Testing;

internal static class PerformanceTestJson {

    public static async Task Run(ILogger debugLogger, TcpUnderlyingConnectionType type) {

        long countPerSecond = 0;
        var server = new TcpServer(new TcpServerOptions() {
            Address = "127.0.0.1",
            Port = 30300,
            ConnectionType = type,
        });

        server.On<PerformanceTestJsonEntity>("math", (entity, conn) => {

            if(entity == null) {
                debugLogger.LogError("Entity null.");
                return;
            }

            conn.Send("math", new PerformanceTestJsonEntityResult() {
                Sum = entity.A + entity.B,
            });

            Interlocked.Increment(ref countPerSecond);

        });

        server.OnConnect += (conn) => {
            debugLogger.LogInformation("{Count} Connections", server.ConnectionCount);
        };

        server.Start();

        for(int e = 0; e < 200; e++) {

            new PerformanceTestJsonWorker(debugLogger, type);

        }

        while(true) {

            await Task.Delay(1000);
            debugLogger.LogInformation("{count} op/s", countPerSecond);

            Interlocked.Exchange(ref countPerSecond, 0);

        }

    }

}

class PerformanceTestJsonWorker {

    private PerformanceTestJsonEntity LastTask { get; set; }

    private TcpClient Client { get; set; }

    public PerformanceTestJsonWorker(ILogger debugLogger, TcpUnderlyingConnectionType type) {

        LastTask = GenerateTask();
        Client = new TcpClient(new TcpClientOptions() {
            Address = "127.0.0.1",
            Port = 30300,
            ConnectionType = type
        });

        Client.On<PerformanceTestJsonEntityResult>("math", (res) => {

            if (res == null) {
                Console.WriteLine("Null found");
                return;
            }

            if (LastTask.A + LastTask.B != res.Sum) {
                debugLogger.LogError("Mismatch found: {LastTask.A} + {LastTask.B} = {res.Sum}", LastTask.A, LastTask.B, res.Sum);
            }

            SendTask();

        });

        Client.OnConnect += () => {

            SendTask();

        };

        Client.Connect();

    }

    private void SendTask() {

        LastTask = GenerateTask();
        Client.Send("math", LastTask);

    }

    private PerformanceTestJsonEntity GenerateTask() {

        return new PerformanceTestJsonEntity() {
            A = RandomUtils.Next(1, 60_000),
            B = RandomUtils.Next(1, 60_000)
        };

    }

}

class PerformanceTestJsonEntity {

    public double A { get; set; }

    public double B { get; set; }

}

class PerformanceTestJsonEntityResult {

    public double Sum { get; set; }

}