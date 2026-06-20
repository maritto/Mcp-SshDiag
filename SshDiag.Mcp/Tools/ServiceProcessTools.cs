using System.ComponentModel;
using ModelContextProtocol.Server;
using SshDiag.Mcp.Security;

namespace SshDiag.Mcp.Tools;

[McpServerToolType]
public sealed class ServiceProcessTools
{
    private readonly ReadOnlyExecutor _executor;

    public ServiceProcessTools(ReadOnlyExecutor executor)
    {
        _executor = executor;
    }

    [McpServerTool, Description("Show service status (systemctl status, read-only).")]
    public static async Task<string> ServiceStatus(
        ReadOnlyExecutor executor,
        [Description("Service name to check")] string service)
    {
        CommandValidator.ValidateServiceName(service);
        var result = await executor.ExecuteAsync("systemctl", new[] { "status", service });
        return FormatResult(result);
    }

    [McpServerTool, Description("List running processes (ps aux), optionally filtered by grep pattern.")]
    public static async Task<string> ListProcesses(
        ReadOnlyExecutor executor,
        [Description("Optional grep pattern to filter processes")] string? filter = null)
    {
        CommandResult result;
        if (!string.IsNullOrEmpty(filter))
        {
            CommandValidator.ValidateSearchPattern(filter);
            result = await executor.ExecuteAsync("sh", new[] { "-c", $"ps aux | grep -i '{filter.Replace("'", "")}'" });
        }
        else
        {
            result = await executor.ExecuteAsync("ps", new[] { "aux" });
        }
        return FormatResult(result);
    }

    [McpServerTool, Description("Show network listeners (ss -tlnp).")]
    public static async Task<string> NetworkListeners(ReadOnlyExecutor executor)
    {
        var result = await executor.ExecuteAsync("ss", new[] { "-tlnp" });
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
