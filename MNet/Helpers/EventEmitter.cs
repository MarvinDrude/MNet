
using System.Linq.Expressions;
using System.Reflection;

namespace MNet.Helpers;

public sealed class EventEmitter(ITcpSerializer serializer) {

    private readonly ITcpSerializer _Serializer = serializer;

    private readonly ConcurrentDictionary<string, Action<ITcpFrame>> _Handlers = new();

    private readonly ConcurrentDictionary<string, Action<ITcpFrame, TcpServerConnection>> _HandlersServer = new();

    public void On<T>(string eventName, Delegate callback) {

        if (_Handlers.ContainsKey(eventName)) {
            throw new InvalidOperationException("Only one handler per event");
        }

        MethodInfo callbackInfo = callback.Method;
        ParameterInfo[] callbackParameters = callbackInfo.GetParameters();

        if(callbackParameters.Length <= 0 || callbackParameters.Length > 2) {
            throw new Exception("Invalid parameter length.");
        }

        bool isSerialize = true;
        if(callbackParameters[0].ParameterType == typeof(ReadOnlyMemory<byte>)) {
            isSerialize = false;
        } else {
            ArgumentOutOfRangeException.ThrowIfNotEqual(callbackParameters[0].ParameterType.IsClass, true, "First parameter wrong type.");
            eventName = TcpConstants.StartSequenceSerialize + eventName;
        }

        bool isServerCallback = false;
        Type typeServerConnection = typeof(TcpServerConnection);

        if(callbackParameters.Length == 2) { // server needs TcpServerConnection 2nd parameter
            isServerCallback = true;
            if(callbackParameters[1].ParameterType != typeServerConnection) {
                throw new ArgumentException("Second parameter wrong type.");
            }
        }

        ParameterExpression[] parameters = callbackParameters.Select((parameter) => {
            return Expression.Parameter(parameter.ParameterType, parameter.Name);
        }).ToArray();

        ParameterExpression paraFrame = Expression.Parameter(typeof(ITcpFrame), "frame");

        MemberExpression memberFrameData = Expression.Property(paraFrame, nameof(ITcpFrame.Data));
        MemberExpression memberFrameDataSpan = Expression.Property(memberFrameData, nameof(ReadOnlyMemory<byte>.Span));

        PropertyInfo dataProperty = typeof(ITcpFrame).GetProperty(nameof(ITcpFrame.Data))!;
        MethodCallExpression dataCall = Expression.Call(paraFrame, dataProperty.GetGetMethod()!);

        PropertyInfo dataSpanProperty = typeof(ReadOnlyMemory<byte>).GetProperty(nameof(ReadOnlyMemory<byte>.Span))!;
        MethodCallExpression dataSpanCall = Expression.Call(memberFrameData, dataSpanProperty.GetGetMethod()!);

        MethodCallExpression resultFirstCall = dataCall; // not serialize, just have the getter function to return readonlymemory

        if(isSerialize) { // if serializable parameter, we have to turn span call into deserialize call

            MethodInfo deserializeMethod = typeof(ITcpSerializer).GetMethod(nameof(ITcpSerializer.Deserialize))!;
            deserializeMethod = deserializeMethod.MakeGenericMethod(typeof(T));

            resultFirstCall = Expression.Call(Expression.Constant(_Serializer), deserializeMethod, dataSpanCall);

        }

        var targetObject = callback.Target;

        if (isServerCallback) {

            var callbackCall = targetObject != null ?
                Expression.Call(Expression.Constant(targetObject), callbackInfo, resultFirstCall, parameters[1]) :
                Expression.Call(callbackInfo, resultFirstCall, parameters[1]);

            var action = Expression.Lambda<Action<ITcpFrame, TcpServerConnection>>
                (callbackCall, paraFrame, parameters[1]).Compile();

            _HandlersServer.TryAdd(eventName, action);

        } else {

            var callbackCall = targetObject != null ?
                Expression.Call(Expression.Constant(targetObject), callbackInfo, resultFirstCall) :
                Expression.Call(callbackInfo, resultFirstCall);

            var action = Expression.Lambda<Action<ITcpFrame>>
                (callbackCall, paraFrame).Compile();

            _Handlers.TryAdd(eventName, action);

        }

    }



    public void Emit(string eventName, ITcpFrame frame) {

        if (!_Handlers.TryGetValue(eventName, out var handler)) {
            return;
        }

        handler(frame);

    }

    public void ServerEmit(string eventName, ITcpFrame frame, TcpServerConnection connection) {

        if (!_HandlersServer.TryGetValue(eventName, out var handler)) {
            return;
        }

        handler(frame, connection);

    }

}

public delegate Task EventDelegateAsync<T>(T? param);

public delegate void EventDelegate<T>(T? param);

public delegate Task ServerEventDelegateAsync<T>(T? param, TcpServerConnection connection);

public delegate void ServerEventDelegate<T>(T? param, TcpServerConnection connection);