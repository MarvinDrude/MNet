
# MNet TCP Server/Client

[NuGet](https://www.nuget.org/packages/MNet)

Just a small lightweight library for TCP Communication in .NET/C#. It utilizes some techniques from internal
kestrel sockets for some performance benefits. Some notes on things used:


## Remarks on used technologies

- Uses some "stackalloc" and "MemoryPool<byte>.Shared" for less heap allocations
- Usage of "System.IO.Pipelines" for better buffering
- Custom schedulers for Tasks
- Custom "PinnedBlockMemoryPool" from kestrel
- Expression Compilation for better performance of events
- For Secure Ssl falls back to SslStream under the hood, but still amazingly fast

## Simple Usage
You should always first register all the event handlers before calling Server.Start/Client.Connect, in order for you to not miss any instant messages.

### Creation of a TCP Server
```csharp
var server = new TcpServer(new TcpServerOptions() {
    Address = "127.0.0.1", 
    Port = 43434,
    Logger = debugLogger, // ILogger of your liking, default is just console one
    Serializer = new TcpJsonSerializer(), // by default TcpJsonSerializer, you can implement your own serializers with ITcpSerializer
    Handshaker = new TcpServerHandshaker(), // by default no handshaking, if you need handshaking implement a ITcpServerHandshaker
});
server.Start();
```

#### Connect / Disconnect (Connect event is after successful handshake)
```csharp
server.OnConnect += (connection) => {
    ...
};

server.OnDisconnect += (connection) => {
    ...
};
```

#### Register event handler for raw bytes messages
```csharp
server.On<ReadOnlyMemory<byte>>("test-bytes", (buffer, connection) => {

    // important, will only work by using ReadOnlyMemory<byte> here, not byte[], Memory<byte> etc.
    Console.WriteLine("Length: " + buffer.Length);

    // send a raw bytes message (important for sending must be of type Memory<byte>)
    connection.Send("test-bytes", new Memory<byte>([0, 2, 3, 5]));

});
```

#### Register event handler for serializable messages
```csharp
server.On<AnyClassOfYours>("test-class", (obj, connection) => {

    if(obj == null) return;
    Console.WriteLine("Length: " + obj.ToString());

    // send a serializable message
    connection.Send("test-class", new AnyClassOfYours() { A = "Wow!" });

});
```

### Creation of a TCP Client
```csharp
var client = new TcpClient(new TcpClientOptions() {
    Address = "127.0.0.1",
    Port = 43434,
    Logger = debugLogger, // ILogger of your liking, default is just console one
    Serializer = new TcpJsonSerializer(), // by default TcpJsonSerializer, you can implement your own serializers with ITcpSerializer
    Handshaker = new TcpClientHandshaker(), // by default no handshaking, if you need handshaking implement a ITcpClientHandshaker
});
client.Connect();
```

#### Connect / Disconnect (Connect event is after successful handshake)
```csharp
client.OnConnect += () => {
    ...
};

client.OnDisconnect += () => {
    ...
};
```

#### Register event handler for raw bytes messages
```csharp
client.On<ReadOnlyMemory<byte>>("test-bytes", (buffer) => {

    // important, will only work by using ReadOnlyMemory<byte> here, not byte[], Memory<byte> etc.
    Console.WriteLine("Length: " + buffer.Length);

    // send a raw bytes message (important for sending must be of type Memory<byte>)
    client.Send("test-bytes", new Memory<byte>([0, 2, 3, 5]));

});
```

#### Register event handler for serializable messages
```csharp
client.On<AnyClassOfYours>("test-class", (obj) => {

    if(obj == null) return;
    Console.WriteLine("Length: " + obj.ToString());

    // send a serializable message
    client.Send("test-class", new AnyClassOfYours() { A = "Wow!" });

});
```

