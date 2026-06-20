namespace SshDiag.Mcp.Security;

public static class CommandValidator
{
    private static readonly HashSet<string> AllowedServices = new(StringComparer.OrdinalIgnoreCase)
    {
        "apache2", "apache", "httpd", "nginx",
        "php7.4-fpm", "php8.0-fpm", "php8.1-fpm", "php8.2-fpm", "php8.3-fpm",
        "mysql", "mariadb", "varnish", "redis", "memcached",
        "postgresql", "docker", "fail2ban"
    };

    private static readonly char[] ForbiddenChars = { ';', '|', '&', '$', '`', '\n', '\r', '>', '<' };

    public static string ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty.");

        if (!path.StartsWith('/'))
            throw new ArgumentException("Path must start with /");

        if (path.Contains(".."))
            throw new ArgumentException("Path traversal (..) is not allowed.");

        if (path.IndexOfAny(ForbiddenChars) >= 0)
            throw new ArgumentException($"Path contains forbidden characters: {string.Join(' ', ForbiddenChars)}");

        return path;
    }

    public static string ValidateServiceName(string service)
    {
        if (string.IsNullOrWhiteSpace(service))
            throw new ArgumentException("Service name cannot be empty.");

        if (!AllowedServices.Contains(service))
            throw new ArgumentException($"Service '{service}' is not in the allowed list. Allowed: {string.Join(", ", AllowedServices)}");

        if (service.IndexOfAny(ForbiddenChars) >= 0)
            throw new ArgumentException("Service name contains forbidden characters.");

        return service;
    }

    public static int ValidateLineCount(int lines, int max = 1000)
    {
        if (lines < 1)
            throw new ArgumentException("Line count must be at least 1.");

        if (lines > max)
            throw new ArgumentException($"Line count cannot exceed {max}.");

        return lines;
    }

    public static string ValidateSearchPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Search pattern cannot be empty.");

        if (pattern.IndexOfAny(ForbiddenChars) >= 0)
            throw new ArgumentException($"Search pattern contains forbidden characters: {string.Join(' ', ForbiddenChars)}");

        return pattern;
    }

    public static string ValidateGlobPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Glob pattern cannot be empty.");

        var forbiddenForGlob = new[] { ';', '|', '&', '$', '`', '>', '<', '\n', '\r' };

        if (pattern.IndexOfAny(forbiddenForGlob) >= 0)
            throw new ArgumentException("Glob pattern contains forbidden characters.");

        return pattern;
    }
}
