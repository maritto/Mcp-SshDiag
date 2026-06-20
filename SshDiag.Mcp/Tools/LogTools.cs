using System.ComponentModel;
using ModelContextProtocol.Server;
using SshDiag.Mcp.Security;

namespace SshDiag.Mcp.Tools;

[McpServerToolType]
public sealed class LogTools
{
    private readonly ReadOnlyExecutor _executor;

    public LogTools(ReadOnlyExecutor executor)
    {
        _executor = executor;
    }

    [McpServerTool, Description("Read last N lines of a log file.")]
    public static async Task<string> ReadLog(
        ReadOnlyExecutor executor,
        [Description("Absolute path to the log file")] string path,
        [Description("Number of lines to read (default 100, max 1000)")] int lines = 100)
    {
        CommandValidator.ValidatePath(path);
        CommandValidator.ValidateLineCount(lines);
        var result = await executor.ExecuteAsync("tail", new[] { "-n", lines.ToString(), path });
        return FormatResult(result);
    }

    [McpServerTool, Description("Read systemd journal for a specific unit (journalctl -u <unit> -n <N> --no-pager).")]
    public static async Task<string> Journalctl(
        ReadOnlyExecutor executor,
        [Description("Systemd unit name (e.g. nginx.service, php8.2-fpm.service)")] string unit,
        [Description("Number of lines to read (default 100, max 1000)")] int lines = 100)
    {
        CommandValidator.ValidateServiceName(unit.Replace(".service", "").Split('@')[0]);
        CommandValidator.ValidateLineCount(lines);
        var result = await executor.ExecuteAsync("journalctl", new[] { "-u", unit, "-n", lines.ToString(), "--no-pager" });
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
