using System.ComponentModel;
using ModelContextProtocol.Server;
using SshDiag.Mcp.Security;

namespace SshDiag.Mcp.Tools;

[McpServerToolType]
public sealed class PhpTools
{
    private readonly ReadOnlyExecutor _executor;

    public PhpTools(ReadOnlyExecutor executor)
    {
        _executor = executor;
    }

    [McpServerTool, Description("Get full PHP configuration dump (php -i).")]
    public static async Task<string> PhpConfig(ReadOnlyExecutor executor)
    {
        var result = await executor.ExecuteAsync("php", new[] { "-i" }, maxOutputLines: 300);
        return FormatResult(result);
    }

    [McpServerTool, Description("List loaded PHP modules (php -m).")]
    public static async Task<string> PhpModules(ReadOnlyExecutor executor)
    {
        var result = await executor.ExecuteAsync("php", new[] { "-m" });
        return FormatResult(result);
    }

    [McpServerTool, Description("Show PHP version (php -v).")]
    public static async Task<string> PhpVersion(ReadOnlyExecutor executor)
    {
        var result = await executor.ExecuteAsync("php", new[] { "-v" });
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
