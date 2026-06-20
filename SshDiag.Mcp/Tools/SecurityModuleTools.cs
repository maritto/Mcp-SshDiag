using System.ComponentModel;
using ModelContextProtocol.Server;
using SshDiag.Mcp.Security;

namespace SshDiag.Mcp.Tools;

[McpServerToolType]
public sealed class SecurityModuleTools
{
    private readonly ReadOnlyExecutor _executor;

    public SecurityModuleTools(ReadOnlyExecutor executor)
    {
        _executor = executor;
    }

    [McpServerTool, Description("Check SELinux enforcement status (getenforce / sestatus).")]
    public static async Task<string> SelinuxStatus(ReadOnlyExecutor executor)
    {
        var getenforce = await executor.ExecuteAsync("getenforce", Array.Empty<string>());
        var sestatus = await executor.ExecuteAsync("sestatus", Array.Empty<string>());

        var output = "=== getenforce ===\n" + getenforce.Output;
        if (!string.IsNullOrEmpty(getenforce.Error))
            output += $"\n[Error: {getenforce.Error}]";

        output += "\n\n=== sestatus ===\n" + sestatus.Output;
        if (!string.IsNullOrEmpty(sestatus.Error))
            output += $"\n[Error: {sestatus.Error}]";

        return output;
    }

    [McpServerTool, Description("Check AppArmor profiles and status (aa-status).")]
    public static async Task<string> ApparmorStatus(ReadOnlyExecutor executor)
    {
        var result = await executor.ExecuteAsync("aa-status", Array.Empty<string>());
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
