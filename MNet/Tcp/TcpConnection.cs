
namespace MNet.Tcp;

internal sealed class TcpConnection {

    private readonly SockConnection Connection;

    private readonly SemaphoreSlim ReceiveSemaphore = new (1, 1);
    private readonly SemaphoreSlim SendSemaphore = new (1, 1);

    private Task? ReceiveTask;
    private Task? SendTask;



    public TcpConnection(SockConnection connection) {

        Connection = connection;

    }

    public void Start() {

        ReceiveTask = DoReceive();
        SendTask = DoSend();

    }

    private async Task DoReceive() {

        while(true) {

            await ReceiveSemaphore.WaitAsync();
            var result = await Connection.Transport.Input.ReadAsync();

            try {

                if(result.Buffer.Length <= 0) {

                    Connection.Transport.Input.AdvanceTo(result.Buffer.Start, result.Buffer.End);

                } else {



                }

                if(result.IsCompleted || result.IsCanceled) {
                    break;
                }

            } catch(Exception) {

            } finally {

                ReceiveSemaphore.Release();

            }

        }

    }

    private async Task DoSend() {



    }

}
