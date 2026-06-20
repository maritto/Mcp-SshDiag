using System.ComponentModel;
using ModelContextProtocol.Server;
using SshDiag.Mcp.Security;

namespace SshDiag.Mcp.Tools;

[McpServerToolType]
public sealed class TextSearchTools
{
    private readonly ReadOnlyExecutor _executor;

    public TextSearchTools(ReadOnlyExecutor executor)
    {
        _executor = executor;
    }

    [McpServerTool, Description("Search file contents recursively (grep -r).")]
    public static async Task<string> SearchFiles(
        ReadOnlyExecutor executor,
        [Description("Search pattern")] string pattern,
        [Description("Absolute directory path to search in")] string path,
        [Description("Maximum number of result lines (default 100, max 1000)")] int maxLines = 100)
    {
        CommandValidator.ValidateSearchPattern(pattern);
        CommandValidator.ValidatePath(path);
        CommandValidator.ValidateLineCount(maxLines);
        var result = await executor.ExecuteAsync("grep", new[] { "-r", pattern, path }, maxOutputLines: maxLines);
        return FormatResult(result);
    }

    [McpServerTool, Description("Search file contents recursively, filtered by filename pattern (grep -r --include).")]
    public static async Task<string> SearchFilesInclude(
        ReadOnlyExecutor executor,
        [Description("Search pattern")] string pattern,
        [Description("Filename glob pattern to include (e.g. *.conf, *.php)")] string include,
        [Description("Absolute directory path to search in")] string path,
        [Description("Maximum number of result lines (default 100, max 1000)")] int maxLines = 100)
    {
        CommandValidator.ValidateSearchPattern(pattern);
        CommandValidator.ValidateGlobPattern(include);
        CommandValidator.ValidatePath(path);
        CommandValidator.ValidateLineCount(maxLines);
        var result = await executor.ExecuteAsync("grep", new[] { "-r", $"--include={include}", pattern, path }, maxOutputLines: maxLines);
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
