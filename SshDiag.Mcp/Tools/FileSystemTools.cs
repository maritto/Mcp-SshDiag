using System.ComponentModel;
using ModelContextProtocol.Server;
using SshDiag.Mcp.Security;

namespace SshDiag.Mcp.Tools;

[McpServerToolType]
public sealed class FileSystemTools
{
    private readonly ReadOnlyExecutor _executor;

    public FileSystemTools(ReadOnlyExecutor executor)
    {
        _executor = executor;
    }

    [McpServerTool, Description("List directory contents with permissions and ownership (ls -la).")]
    public static async Task<string> ListDir(
        ReadOnlyExecutor executor,
        [Description("Absolute directory path to list")] string path)
    {
        CommandValidator.ValidatePath(path);
        var result = await executor.ExecuteAsync("ls", new[] { "-la", path });
        return FormatResult(result);
    }

    [McpServerTool, Description("Trace full path permissions from root to target (namei -l).")]
    public static async Task<string> PathPermissions(
        ReadOnlyExecutor executor,
        [Description("Absolute path to trace")] string path)
    {
        CommandValidator.ValidatePath(path);
        var result = await executor.ExecuteAsync("namei", new[] { "-l", path });
        return FormatResult(result);
    }

    [McpServerTool, Description("Show file/directory metadata (stat).")]
    public static async Task<string> FileInfo(
        ReadOnlyExecutor executor,
        [Description("Absolute path to examine")] string path)
    {
        CommandValidator.ValidatePath(path);
        var result = await executor.ExecuteAsync("stat", new[] { path });
        return FormatResult(result);
    }

    [McpServerTool, Description("Search for files by name pattern (find <path> -name <pattern>).")]
    public static async Task<string> FindFiles(
        ReadOnlyExecutor executor,
        [Description("Absolute directory path to search in")] string path,
        [Description("Filename glob pattern (e.g. *.log, config.*)")] string pattern,
        [Description("Maximum search depth (default 5)")] int maxDepth = 5)
    {
        CommandValidator.ValidatePath(path);
        CommandValidator.ValidateGlobPattern(pattern);
        var result = await executor.ExecuteAsync("find", new[] { path, "-maxdepth", maxDepth.ToString(), "-name", pattern });
        return FormatResult(result);
    }

    [McpServerTool, Description("Detect file type using magic bytes (file command).")]
    public static async Task<string> FileType(
        ReadOnlyExecutor executor,
        [Description("Absolute path to the file")] string path)
    {
        CommandValidator.ValidatePath(path);
        var result = await executor.ExecuteAsync("file", new[] { path });
        return FormatResult(result);
    }

    [McpServerTool, Description("Show directory size (du -sh).")]
    public static async Task<string> DirSize(
        ReadOnlyExecutor executor,
        [Description("Absolute directory path")] string path)
    {
        CommandValidator.ValidatePath(path);
        var result = await executor.ExecuteAsync("du", new[] { "-sh", path });
        return FormatResult(result);
    }

    [McpServerTool, Description("Show user/group membership info (id).")]
    public static async Task<string> UserInfo(
        ReadOnlyExecutor executor,
        [Description("Username to look up (optional, defaults to current user)")] string? user = null)
    {
        var args = user is not null
            ? new[] { CommandValidator.ValidateSearchPattern(user) }
            : Array.Empty<string>();
        var result = await executor.ExecuteAsync("id", args);
        return FormatResult(result);
    }

    [McpServerTool, Description("Read full file contents (cat). Use for small files only.")]
    public static async Task<string> ReadFile(
        ReadOnlyExecutor executor,
        [Description("Absolute file path to read")] string path)
    {
        CommandValidator.ValidatePath(path);
        var result = await executor.ExecuteAsync("cat", new[] { path });
        return FormatResult(result);
    }

    [McpServerTool, Description("Read first N lines of a file (head).")]
    public static async Task<string> ReadFileHead(
        ReadOnlyExecutor executor,
        [Description("Absolute file path")] string path,
        [Description("Number of lines to read (default 50, max 1000)")] int lines = 50)
    {
        CommandValidator.ValidatePath(path);
        CommandValidator.ValidateLineCount(lines);
        var result = await executor.ExecuteAsync("head", new[] { "-n", lines.ToString(), path });
        return FormatResult(result);
    }

    [McpServerTool, Description("Read last N lines of a file (tail).")]
    public static async Task<string> ReadFileTail(
        ReadOnlyExecutor executor,
        [Description("Absolute file path")] string path,
        [Description("Number of lines to read (default 50, max 1000)")] int lines = 50)
    {
        CommandValidator.ValidatePath(path);
        CommandValidator.ValidateLineCount(lines);
        var result = await executor.ExecuteAsync("tail", new[] { "-n", lines.ToString(), path });
        return FormatResult(result);
    }

    [McpServerTool, Description("Count lines in a file (wc -l).")]
    public static async Task<string> CountLines(
        ReadOnlyExecutor executor,
        [Description("Absolute file path")] string path)
    {
        CommandValidator.ValidatePath(path);
        var result = await executor.ExecuteAsync("wc", new[] { "-l", path });
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
