
namespace MNet.Internal.Factories;

internal interface IConnectionFactory : IDisposable {

    public IDuplexPipe Create(Socket socket, Stream? stream);

}
