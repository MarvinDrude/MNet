
namespace MNet.Tcp.Options;

public sealed class TcpClientOptions : TcpOptions {

    /// <summary>
    /// Needed for secure authentication
    /// </summary>
    public string? Host {  get; set; }

    /// <summary>
    /// Default interval is 3 seconds
    /// </summary>
    public TimeSpan ReconnectInterval { get; set; } = TimeSpan.FromSeconds(3);

}
