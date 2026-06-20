using System.Text;
using Renci.SshNet;

namespace SshDiag.Mcp.Ssh;

public class SshConnectionManager
{
    private SshClient? _client;
    private SshConnectionOptions? _options;
    private readonly object _lock = new();

    public bool IsConnected
    {
        get
        {
            lock (_lock)
            {
                return _client?.IsConnected ?? false;
            }
        }
    }

    public string? ConnectedHost => _options?.Host;

    public Task ConnectAsync(SshConnectionOptions options)
    {
        lock (_lock)
        {
            if (_client?.IsConnected == true)
                throw new InvalidOperationException($"Already connected to {_options?.Host}");

            var connectionInfo = BuildConnectionInfo(options);
            _client = new SshClient(connectionInfo);
            _client.Connect();
            _options = options;
        }

        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        lock (_lock)
        {
            if (_client?.IsConnected == true)
            {
                _client.Disconnect();
                _client.Dispose();
            }

            _client = null;
            _options = null;
        }

        return Task.CompletedTask;
    }

    public SshCommand RunCommand(string commandText)
    {
        SshClient client;
        lock (_lock)
        {
            if (_client is null || !_client.IsConnected)
                throw new InvalidOperationException("Not connected to any SSH server. Use ssh_connect first.");
            client = _client;
        }

        return client.CreateCommand(commandText);
    }

    public async Task EnsureConnectedAsync()
    {
        lock (_lock)
        {
            if (_client is null)
                throw new InvalidOperationException("Not connected to any SSH server. Use ssh_connect first.");

            if (!_client.IsConnected)
            {
                try
                {
                    _client.Connect();
                }
                catch
                {
                    if (_options is not null)
                    {
                        var connectionInfo = BuildConnectionInfo(_options);
                        _client.Dispose();
                        _client = new SshClient(connectionInfo);
                        _client.Connect();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        await Task.CompletedTask;
    }

    private static ConnectionInfo BuildConnectionInfo(SshConnectionOptions options)
    {
        if (!string.IsNullOrEmpty(options.Password))
        {
            return new ConnectionInfo(options.Host, options.Port, options.Username,
                new PasswordAuthenticationMethod(options.Username, options.Password));
        }

        if (!string.IsNullOrEmpty(options.PrivateKeyPath))
        {
            var keyFile = string.IsNullOrEmpty(options.PrivateKeyPassphrase)
                ? new PrivateKeyFile(options.PrivateKeyPath)
                : new PrivateKeyFile(options.PrivateKeyPath, options.PrivateKeyPassphrase);

            return new ConnectionInfo(options.Host, options.Port, options.Username,
                new PrivateKeyAuthenticationMethod(options.Username, keyFile));
        }

        if (!string.IsNullOrEmpty(options.PrivateKeyContent))
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(options.PrivateKeyContent));
            var keyFile = string.IsNullOrEmpty(options.PrivateKeyPassphrase)
                ? new PrivateKeyFile(stream)
                : new PrivateKeyFile(stream, options.PrivateKeyPassphrase);

            return new ConnectionInfo(options.Host, options.Port, options.Username,
                new PrivateKeyAuthenticationMethod(options.Username, keyFile));
        }

        throw new ArgumentException("Either Password, PrivateKeyPath, or PrivateKeyContent must be provided.");
    }
}
