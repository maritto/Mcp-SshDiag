using System.ComponentModel;
using ModelContextProtocol.Server;
using SshDiag.Mcp.Configuration;
using SshDiag.Mcp.Ssh;

namespace SshDiag.Mcp.Tools;

[McpServerToolType]
public sealed class ConnectionTools
{
    private readonly SshConnectionManager _connection;

    public ConnectionTools(SshConnectionManager connection)
    {
        _connection = connection;
    }

    [McpServerTool, Description("Connect to a remote server via SSH. Supports env var defaults: SSH_DIAG_HOST, SSH_DIAG_PORT, SSH_DIAG_USER, SSH_DIAG_PASSWORD, SSH_DIAG_KEY_PATH, SSH_DIAG_KEY_PASSPHRASE.")]
    public static async Task<string> SshConnect(
        SshConnectionManager connection,
        SshDiagConfig config,
        [Description("Hostname or IP address of the server")] string host = "",
        [Description("SSH port (default 22)")] int port = 0,
        [Description("SSH username")] string username = "",
        [Description("Password for authentication (use password OR keyPath, not both)")] string? password = null,
        [Description("Path to private key file for authentication")] string? keyPath = null,
        [Description("Passphrase for the private key (if encrypted)")] string? keyPassphrase = null)
    {
        var effectiveHost = !string.IsNullOrEmpty(host) ? host : config.DefaultHost ?? "";
        var effectivePort = port > 0 ? port : config.DefaultPort;
        var effectiveUser = !string.IsNullOrEmpty(username) ? username : config.DefaultUsername ?? "";
        var effectivePassword = password ?? config.DefaultPassword;
        var effectiveKeyPath = keyPath ?? config.DefaultKeyPath;
        var effectiveKeyPassphrase = keyPassphrase ?? config.DefaultKeyPassphrase;

        if (string.IsNullOrEmpty(effectiveHost))
            return "Error: host is required (pass as parameter or set SSH_DIAG_HOST env var)";
        if (string.IsNullOrEmpty(effectiveUser))
            return "Error: username is required (pass as parameter or set SSH_DIAG_USER env var)";
        if (string.IsNullOrEmpty(effectivePassword) && string.IsNullOrEmpty(effectiveKeyPath))
            return "Error: password or keyPath is required (pass as parameter or set SSH_DIAG_PASSWORD or SSH_DIAG_KEY_PATH env var)";

        var options = new SshConnectionOptions
        {
            Host = effectiveHost,
            Port = effectivePort,
            Username = effectiveUser,
            Password = effectivePassword,
            PrivateKeyPath = effectiveKeyPath,
            PrivateKeyPassphrase = effectiveKeyPassphrase
        };

        try
        {
            await connection.ConnectAsync(options);
            return $"Connected to {host}:{port} as {username}";
        }
        catch (Exception ex)
        {
            return $"Connection failed: {ex.Message}";
        }
    }

    [McpServerTool, Description("Disconnect from the remote SSH server.")]
    public static async Task<string> SshDisconnect(SshConnectionManager connection)
    {
        var host = connection.ConnectedHost;
        await connection.DisconnectAsync();
        return host is not null ? $"Disconnected from {host}" : "Not connected to any server";
    }

    [McpServerTool, Description("Show current SSH connection status.")]
    public static string SshStatus(SshConnectionManager connection)
    {
        if (connection.IsConnected)
            return $"Connected to {connection.ConnectedHost}";
        return "Not connected to any server. Use ssh_connect first.";
    }
}
