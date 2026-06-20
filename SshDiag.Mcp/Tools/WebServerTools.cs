using System.ComponentModel;
using ModelContextProtocol.Server;
using SshDiag.Mcp.Security;

namespace SshDiag.Mcp.Tools;

[McpServerToolType]
public sealed class WebServerTools
{
    private readonly ReadOnlyExecutor _executor;

    public WebServerTools(ReadOnlyExecutor executor)
    {
        _executor = executor;
    }

    [McpServerTool, Description("Dump Apache virtual host layout (apache2ctl -S).")]
    public static async Task<string> ApacheConfigDump(ReadOnlyExecutor executor)
    {
        var result = await executor.ExecuteAsync("apache2ctl", new[] { "-S" });
        return FormatResult(result);
    }

    [McpServerTool, Description("Show Apache compile-time settings (apache2ctl -V).")]
    public static async Task<string> ApacheBuildInfo(ReadOnlyExecutor executor)
    {
        var result = await executor.ExecuteAsync("apache2ctl", new[] { "-V" });
        return FormatResult(result);
    }

    [McpServerTool, Description("Dump full nginx configuration (nginx -T).")]
    public static async Task<string> NginxConfigDump(ReadOnlyExecutor executor)
    {
        var result = await executor.ExecuteAsync("nginx", new[] { "-T" }, maxOutputLines: 500);
        return FormatResult(result);
    }

    private static string FormatResult(CommandResult result)
    {
        var output = result.Output;
        if (!string.IsNullOrEmpty(result.Error) && string.IsNullOrEmpty(result.Output))
            output = $"ERROR: {result.Error}";
        if (result.Truncated)
            output += "\n[Output truncated]";
        if (result.ExitCode != 0 && !string.IsNullOrEmpty(result.Error))
            output += $"\n[Exit code: {result.ExitCode}] {result.Error}";
        return string.IsNullOrEmpty(output) ? "(no output)" : output;
    }
}
