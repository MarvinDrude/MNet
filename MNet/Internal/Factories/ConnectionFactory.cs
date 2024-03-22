
namespace MNet.Internal.Factories;

internal abstract class ConnectionFactory<TOptions, TSettings, TConnection> : IDisposable
    where TOptions : ConnectionOptions
    where TSettings : ConnectionQueueSettings
    where TConnection : IDuplexPipe {

    protected readonly TOptions _Options;

    protected readonly int _SettingsCount;

    protected readonly TSettings[] _Settings;

    protected long _SettingsIndex;

    private bool _AlreadyDisposed = false;

    public ConnectionFactory(TOptions options) {

        _Options = options;
        _SettingsCount = options.IOQueueCount;
        _SettingsIndex = 0;

        _Settings = new TSettings[_SettingsCount];
        InitSettings();
        
    }

    public TConnection Create(Socket socket, Stream? stream) {

        var setting = _Settings[Interlocked.Increment(ref _SettingsIndex) % _SettingsCount];

        return CreateConnection(socket, stream, setting);

    }

    protected abstract TConnection CreateConnection(Socket socket, Stream? stream, TSettings settings);

    private void InitSettings() {

        for(int e = 0; e < _SettingsCount; e++) {

            _Settings[e] = Unsafe.As<TSettings>(_Options.CreateQueueSettings());

        }

    }

    public void Dispose() {

        if(_AlreadyDisposed) {
            return;
        }

        foreach (var setting in _Settings) {

            setting?.Dispose();

        }

        _AlreadyDisposed = true;

    }

}
