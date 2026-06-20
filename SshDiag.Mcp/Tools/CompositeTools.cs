using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;
using SshDiag.Mcp.Security;

namespace SshDiag.Mcp.Tools;

[McpServerToolType]
public sealed class CompositeTools
{
    private readonly ReadOnlyExecutor _executor;

    public CompositeTools(ReadOnlyExecutor executor)
    {
        _executor = executor;
    }

    [McpServerTool, Description("Check PHP upload limits across all SAPIs and highlight mismatches.")]
    public static async Task<string> CheckPhpUploadLimits(
        ReadOnlyExecutor executor,
        [Description("Optional path to check for .user.ini overrides")] string? webRoot = null)
    {
        var sb = new StringBuilder();
        var settings = new[] { "upload_max_filesize", "post_max_size", "memory_limit", "max_execution_time", "sys_temp_dir" };
        var sapis = new[] { "cli", "apache2", "fpm" };

        sb.AppendLine("=== PHP Upload Limits Check ===\n");

        foreach (var sapi in sapis)
        {
            sb.AppendLine($"--- SAPI: {sapi} ---");
            var findResult = await executor.ExecuteAsync("sh", new[] { "-c", $"ls -d /etc/php/*/php.ini /etc/php/*/{sapi}/php.ini 2>/dev/null" });

            if (string.IsNullOrWhiteSpace(findResult.Output) || findResult.Output.Contains("No such file"))
            {
                sb.AppendLine("  (no php.ini found for this SAPI)\n");
                continue;
            }

            var iniFiles = findResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var iniFile in iniFiles)
            {
                var trimmedFile = iniFile.Trim();
                if (string.IsNullOrEmpty(trimmedFile)) continue;
                sb.AppendLine($"  Config: {trimmedFile}");
                foreach (var setting in settings)
                {
                    var grepResult = await executor.ExecuteAsync("grep", new[] { "-i", $"^{setting}\\s*=", trimmedFile });
                    if (!string.IsNullOrWhiteSpace(grepResult.Output))
                        sb.AppendLine($"    {grepResult.Output.Trim()}");
                }
                sb.AppendLine();
            }
        }

        if (!string.IsNullOrEmpty(webRoot))
        {
            CommandValidator.ValidatePath(webRoot);
            sb.AppendLine($"--- .user.ini overrides in {webRoot} ---");
            var userIni = await executor.ExecuteAsync("find", new[] { webRoot, "-maxdepth", "3", "-name", ".user.ini" });
            if (!string.IsNullOrWhiteSpace(userIni.Output))
            {
                var files = userIni.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var file in files)
                {
                    var trimmed = file.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    sb.AppendLine($"  File: {trimmed}");
                    var catResult = await executor.ExecuteAsync("cat", new[] { trimmed });
                    sb.AppendLine(catResult.Output);
                }
            }
            else
            {
                sb.AppendLine("  (no .user.ini files found)");
            }
        }

        sb.AppendLine("\n=== Common Mismatch Warnings ===");
        sb.AppendLine("- If post_max_size < upload_max_filesize: uploads will fail at post_max_size limit");
        sb.AppendLine("- If memory_limit < post_max_size: large uploads may exhaust memory");
        sb.AppendLine("- Check .user.ini files for per-directory overrides");

        return sb.ToString();
    }

    [McpServerTool, Description("Trace full path permissions and check owner matches web server user.")]
    public static async Task<string> CheckPermissions(
        ReadOnlyExecutor executor,
        [Description("Absolute path to check permissions for")] string path)
    {
        CommandValidator.ValidatePath(path);
        var sb = new StringBuilder();

        sb.AppendLine($"=== Permission Check for {path} ===\n");

        sb.AppendLine("--- namei -l (full path trace) ---");
        var namei = await executor.ExecuteAsync("namei", new[] { "-l", path });
        sb.AppendLine(namei.Output);

        sb.AppendLine("\n--- ls -la (directory listing) ---");
        var parentPath = path.EndsWith('/') ? path : path + "/";
        var ls = await executor.ExecuteAsync("ls", new[] { "-la", parentPath });
        sb.AppendLine(ls.Output);

        sb.AppendLine("\n--- Web server user check ---");
        var psWww = await executor.ExecuteAsync("sh", new[] { "-c", "ps -eo user,comm | grep -E 'apache|nginx|httpd|www-data' | sort -u" });
        if (!string.IsNullOrWhiteSpace(psWww.Output))
        {
            sb.AppendLine("Web server processes running as:");
            sb.AppendLine(psWww.Output);
        }
        else
        {
            sb.AppendLine("No web server processes found running.");
        }

        return sb.ToString();
    }

    [McpServerTool, Description("Check web server body size limits (LimitRequestBody / client_max_body_size).")]
    public static async Task<string> CheckWebserverLimits(ReadOnlyExecutor executor)
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== Web Server Body Size Limits ===\n");

        sb.AppendLine("--- Apache LimitRequestBody ---");
        var apacheGrep = await executor.ExecuteAsync("grep", new[] { "-r", "LimitRequestBody", "/etc/apache2/" }, maxOutputLines: 50);
        if (!string.IsNullOrWhiteSpace(apacheGrep.Output))
            sb.AppendLine(apacheGrep.Output);
        else
            sb.AppendLine("(no LimitRequestBody found in /etc/apache2/)");

        sb.AppendLine("\n--- nginx client_max_body_size ---");
        var nginxGrep = await executor.ExecuteAsync("grep", new[] { "-r", "client_max_body_size", "/etc/nginx/" }, maxOutputLines: 50);
        if (!string.IsNullOrWhiteSpace(nginxGrep.Output))
            sb.AppendLine(nginxGrep.Output);
        else
            sb.AppendLine("(no client_max_body_size found in /etc/nginx/)");

        sb.AppendLine("\nNote: If no limits are explicitly set, defaults apply:");
        sb.AppendLine("  Apache: no limit (unlimited request body)");
        sb.AppendLine("  nginx: 1m (1 megabyte)");

        return sb.ToString();
    }

    [McpServerTool, Description("Check if SELinux or AppArmor might be blocking writes.")]
    public static async Task<string> CheckSecurityModules(ReadOnlyExecutor executor)
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== Security Modules Check ===\n");

        sb.AppendLine("--- SELinux ---");
        var getenforce = await executor.ExecuteAsync("getenforce", Array.Empty<string>());
        if (!string.IsNullOrWhiteSpace(getenforce.Output))
        {
            sb.AppendLine($"getenforce: {getenforce.Output.Trim()}");
            if (getenforce.Output.Trim().Equals("Enforcing", StringComparison.OrdinalIgnoreCase))
                sb.AppendLine("WARNING: SELinux is ENFORCING - may block file writes!");
        }
        else
        {
            sb.AppendLine("(SELinux not installed or getenforce not available)");
        }

        sb.AppendLine("\n--- AppArmor ---");
        var aaStatus = await executor.ExecuteAsync("aa-status", Array.Empty<string>());
        if (!string.IsNullOrWhiteSpace(aaStatus.Output))
        {
            sb.AppendLine(aaStatus.Output);
            if (aaStatus.Output.Contains("enforce", StringComparison.OrdinalIgnoreCase))
                sb.AppendLine("\nWARNING: AppArmor has profiles in enforce mode - may block file writes!");
        }
        else
        {
            sb.AppendLine("(AppArmor not installed or aa-status not available)");
        }

        return sb.ToString();
    }

    [McpServerTool, Description("Quick disk space and inode check, warns if >90% usage.")]
    public static async Task<string> CheckDiskSpace(ReadOnlyExecutor executor)
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== Disk Space Check ===\n");

        sb.AppendLine("--- Disk usage (df -h) ---");
        var dfh = await executor.ExecuteAsync("df", new[] { "-h" });
        sb.AppendLine(dfh.Output);

        sb.AppendLine("\n--- Inode usage (df -i) ---");
        var dfi = await executor.ExecuteAsync("df", new[] { "-i" });
        sb.AppendLine(dfi.Output);

        sb.AppendLine("\n--- Warnings (>90% usage) ---");
        var warnDisk = await executor.ExecuteAsync("sh", new[] { "-c", "df -h | awk 'NR>1 && $5+0 > 90 {print $0}'" });
        var warnInode = await executor.ExecuteAsync("sh", new[] { "-c", "df -i | awk 'NR>1 && $5+0 > 90 {print $0}'" });

        var hasWarning = false;
        if (!string.IsNullOrWhiteSpace(warnDisk.Output))
        {
            sb.AppendLine("Disk space >90%:");
            sb.AppendLine(warnDisk.Output);
            hasWarning = true;
        }
        if (!string.IsNullOrWhiteSpace(warnInode.Output))
        {
            sb.AppendLine("Inode usage >90%:");
            sb.AppendLine(warnInode.Output);
            hasWarning = true;
        }
        if (!hasWarning)
        {
            sb.AppendLine("(no partitions above 90% usage)");
        }

        return sb.ToString();
    }

    [McpServerTool, Description("Search common log sources for error patterns (upload, 413, 500, too large).")]
    public static async Task<string> SearchErrors(ReadOnlyExecutor executor)
    {
        var sb = new StringBuilder();
        var patterns = "error\\|fail\\|413\\|500\\|exceeds\\|too large";

        sb.AppendLine("=== Error Search Across Log Sources ===\n");

        var logSources = new[]
        {
            ("/var/log/apache2/error.log", "Apache error log"),
            ("/var/log/nginx/error.log", "nginx error log"),
        };

        foreach (var (logPath, label) in logSources)
        {
            sb.AppendLine($"--- {label} ({logPath}) ---");
            var result = await executor.ExecuteAsync("grep", new[] { "-i", patterns, logPath }, maxOutputLines: 50);
            if (!string.IsNullOrWhiteSpace(result.Output))
                sb.AppendLine(result.Output);
            else
                sb.AppendLine("(no matching errors found or file not accessible)");
            sb.AppendLine();
        }

        sb.AppendLine("--- PHP-FPM journal errors ---");
        var phpFpm = await executor.ExecuteAsync("sh", new[] { "-c", "journalctl -u 'php*-fpm' --no-pager -n 100 2>/dev/null | grep -i 'error\\|fail\\|warn'" }, maxOutputLines: 50);
        if (!string.IsNullOrWhiteSpace(phpFpm.Output))
            sb.AppendLine(phpFpm.Output);
        else
            sb.AppendLine("(no PHP-FPM errors found in journal)");

        return sb.ToString();
    }
}
