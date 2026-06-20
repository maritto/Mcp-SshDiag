namespace SshDiag.Mcp.Configuration;

public class SshDiagConfig
{
    public int DefaultMaxOutputLines { get; set; } = 500;
    public int CommandTimeoutSeconds { get; set; } = 30;

    public string? DefaultHost { get; set; }
    public int DefaultPort { get; set; } = 22;
    public string? DefaultUsername { get; set; }
    public string? DefaultPassword { get; set; }
    public string? DefaultKeyPath { get; set; }
    public string? DefaultKeyPassphrase { get; set; }
}
