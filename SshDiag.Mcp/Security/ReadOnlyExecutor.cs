using System.Diagnostics;
using SshDiag.Mcp.Configuration;
using SshDiag.Mcp.Ssh;

namespace SshDiag.Mcp.Security;

public class ReadOnlyExecutor
{
    private readonly SshConnectionManager _connection;
    private readonly SshDiagConfig _config;

    public ReadOnlyExecutor(SshConnectionManager connection, SshDiagConfig config)
    {
        _connection = connection;
        _config = config;
    }

    public async Task<CommandResult> ExecuteAsync(
        string command,
        string[] arguments,
        int? maxOutputLines = null,
        TimeSpan? timeout = null)
    {
        await _connection.EnsureConnectedAsync();

        var fullCommand = BuildCommand(command, arguments);
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(_config.CommandTimeoutSeconds);
        var maxLines = maxOutputLines ?? _config.DefaultMaxOutputLines;

        var cmd = _connection.RunCommand(fullCommand);

        var task = Task.Run(() =>
        {
            var result = cmd.Execute();
            return result;
        });

        var completed = await Task.WhenAny(task, Task.Delay(effectiveTimeout));

        if (completed != task)
        {
            cmd.CancelAsync();
            return new CommandResult(fullCommand, "", "Command timed out", -1, false);
        }

        var output = task.Result;
        var (truncatedOutput, wasTruncated) = TruncateOutput(output, maxLines);

        return new CommandResult(
            fullCommand,
            truncatedOutput,
            string.IsNullOrEmpty(cmd.Error) ? null : cmd.Error,
            cmd.ExitStatus ?? -1,
            wasTruncated
        );
    }

    private static string BuildCommand(string command, string[] arguments)
    {
        if (arguments.Length == 0)
            return command;

        var escaped = arguments.Select(a => $"'{a.Replace("'", "'\\''")}'");
        return $"{command} {string.Join(' ', escaped)}";
    }

    private static (string output, bool truncated) TruncateOutput(string output, int maxLines)
    {
        var lines = output.Split('\n');
        if (lines.Length <= maxLines)
            return (output, false);

        return (string.Join('\n', lines.Take(maxLines)) + $"\n... truncated ({lines.Length} total lines)", true);
    }
}

public record CommandResult(
    string Command,
    string Output,
    string? Error,
    int ExitCode,
    bool Truncated
);
