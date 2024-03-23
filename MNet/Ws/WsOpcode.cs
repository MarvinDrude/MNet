
namespace MNet.Ws;

//https://tools.ietf.org/html/rfc6455#section-11.8
public enum WsOpcode {

    ContinuationFrame = 0,
    TextFrame = 1,
    BinaryFrame = 2,
    ConnectionCloseFrame = 8,
    PingFrame = 9,
    PongFrame = 10

}
