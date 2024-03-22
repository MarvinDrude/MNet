
namespace MNet.Internal.Factories;

internal interface IConnectionFactory {

    public IDuplexPipe Create(Socket socket, Stream? stream);

}
