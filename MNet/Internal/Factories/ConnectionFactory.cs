


namespace MNet.Internal.Factories;

internal class ConnectionFactory<TOptions, TSettings> : IDisposable
    where TOptions : ConnectionOptions
    where TSettings : ConnectionQueueSettings {

    protected readonly TOptions _Options;

    protected readonly int _SettingsCount;

    protected readonly TSettings[] _Settings;

    protected readonly long _SettingsIndex;

    public ConnectionFactory(TOptions options) {

        _Options = options;
        _SettingsCount = options.IOQueueCount;
        _SettingsIndex = 0;

        _Settings = new TSettings[_SettingsCount];
        InitSettings();
        
    }

    private void InitSettings() {

        var maxReadBufferSize = _Options.MaxReadBufferSize ?? 0;
        var maxWriteBufferSize = _Options.MaxWriteBufferSize ?? 0;

        for(int e = 0; e < _SettingsCount; e++) {



        }

    }

}
