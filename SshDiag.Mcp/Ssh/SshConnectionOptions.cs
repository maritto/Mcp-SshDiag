namespace SshDiag.Mcp.Ssh;

public class SshConnectionOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 22;
    public string Username { get; set; } = "";
    public string? Password { get; set; }
    public string? PrivateKeyPath { get; set; }
    public string? PrivateKeyPassphrase { get; set; }
    public string? PrivateKeyContent { get; set; }
}
