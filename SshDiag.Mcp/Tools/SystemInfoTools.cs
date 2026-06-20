using System.ComponentModel;
using ModelContextProtocol.Server;
using SshDiag.Mcp.Security;

namespace SshDiag.Mcp.Tools;

[McpServerToolType]
public sealed class SystemInfoTools
{
    private readonly ReadOnlyExecutor _executor;

    public SystemInfoTools(ReadOnlyExecutor executor)
    {
        _executor = executor;
    }

    [McpServerTool, Description("Get OS, kernel, and architecture info (uname -a).")]
    public static async Task<string> SysInfo(ReadOnlyExecutor executor)
    {
        var result = await executor.ExecuteAsync("uname", new[] { "-a" });
        return FormatResult(result);
    }

    [McpServerTool, Description("Show disk space usage (df -h).")]
    public static async Task<string> DiskSpace(ReadOnlyExecutor executor)
    {
        var result = await executor.ExecuteAsync("df", new[] { "-h" });
        return FormatResult(result);
    }

    [McpServerTool, Description("Show inode usage (df -i).")]
    public static async Task<string> DiskInodes(ReadOnlyExecutor executor)
    {
        var result = await executor.ExecuteAsync("df", new[] { "-i" });
        return FormatResult(result);
    }

    [McpServerTool, Description("Show RAM and swap usage (free -h).")]
    public static async Task<string> MemoryInfo(ReadOnlyExecutor executor)
    {
        var result = await executor.ExecuteAsync("free", new[] { "-h" });
        return FormatResult(result);
    }

    [McpServerTool, Description("Show system load averages and uptime.")]
    public static async Task<string> LoadAvg(ReadOnlyExecutor executor)
    {
        var result = await executor.ExecuteAsync("uptime", Array.Empty<string>());
        return FormatResult(result);
    }

    [McpServerTool, Description("List installed packages (dpkg -l), optionally filtered by a grep pattern.")]
    public static async Task<string> InstalledPackages(
        ReadOnlyExecutor executor,
        [Description("Optional grep pattern to filter package names")] string? filter = null)
    {
        CommandResult result;
        if (!string.IsNullOrEmpty(filter))
        {
            CommandValidator.ValidateSearchPattern(filter);
            result = await executor.ExecuteAsync("sh", new[] { "-c", $"dpkg -l | grep -i '{filter.Replace("'", "")}'" });
        }
        else
        {
            result = await executor.ExecuteAsync("dpkg", new[] { "-l" }, maxOutputLines: 200);
        }
        return FormatResult(result);
    }

    [McpServerTool, Description("Show details for a specific package (apt show).")]
    public static async Task<string> PackageInfo(
        ReadOnlyExecutor executor,
        [Description("Package name to look up")] string package)
    {
        CommandValidator.ValidateSearchPattern(package);
        var result = await executor.ExecuteAsync("apt", new[] { "show", package });
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
