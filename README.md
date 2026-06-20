# SSH Diagnostic MCP

A read-only SSH diagnostic MCP server for AI-powered server troubleshooting. Connect to any Linux server via SSH and run safe, read-only diagnostic commands through your AI assistant.

## Features

- **Read-only by design** — no write, modify, or destructive operations
- **Input validation** — all paths, patterns, and service names are sanitized to prevent injection
- **30+ diagnostic tools** across 10 categories
- **Environment variable defaults** — pre-configure connection details so you don't pass credentials in chat
- **Auto-reconnect** — gracefully handles dropped SSH connections

## Tool Categories

| Category | Tools |
|---|---|
| **Connection** | `ssh_connect`, `ssh_disconnect`, `ssh_status` |
| **System Info** | `sys_info`, `disk_space`, `disk_inodes`, `memory_info`, `load_avg`, `installed_packages`, `package_info` |
| **File System** | `list_dir`, `path_permissions`, `file_info`, `find_files`, `file_type`, `dir_size`, `user_info`, `read_file`, `read_file_head`, `read_file_tail`, `count_lines` |
| **Text Search** | `search_files`, `search_files_include` |
| **PHP** | `php_config`, `php_modules`, `php_version` |
| **Web Server** | `apache_config_dump`, `apache_build_info`, `nginx_config_dump` |
| **Services** | `service_status`, `list_processes`, `network_listeners` |
| **Security** | `selinux_status`, `apparmor_status` |
| **Logs** | `read_log`, `journalctl` |
| **Composite** | `check_php_upload_limits`, `check_permissions`, `check_webserver_limits`, `check_security_modules`, `check_disk_space`, `search_errors` |

## Prerequisites

- .NET 8 SDK
- An MCP client (e.g., [opencode](https://opencode.ai))

## Setup

### 1. Build

```bash
dotnet build SshDiag.Mcp
```

### 2. Configure environment variables

Set these system environment variables with your SSH credentials:

| Variable | Description | Required |
|---|---|---|
| `SSH_DIAG_HOST` | Default hostname or IP | Yes* |
| `SSH_DIAG_USER` | Default username | Yes* |
| `SSH_DIAG_PASSWORD` | Password for authentication | Yes* (or key) |
| `SSH_DIAG_KEY_PATH` | Path to private key file | Yes* (or password) |
| `SSH_DIAG_KEY_PASSPHRASE` | Passphrase for encrypted key | No |
| `SSH_DIAG_PORT` | Default SSH port (default: 22) | No |

\* Can also be passed as parameters to `ssh_connect` at runtime.

**Windows example:**
```powershell
[Environment]::SetEnvironmentVariable("SSH_DIAG_HOST", "192.168.1.20", "Machine")
[Environment]::SetEnvironmentVariable("SSH_DIAG_USER", "root", "Machine")
[Environment]::SetEnvironmentVariable("SSH_DIAG_PASSWORD", "your-password", "Machine")
```

**Linux/macOS example:**
```bash
export SSH_DIAG_HOST=192.168.1.20
export SSH_DIAG_USER=root
export SSH_DIAG_PASSWORD=your-password
```

### 3. Add to opencode

Create an `opencode.json` in your project root:

```json
{
  "mcp": {
    "ssh-diag": {
      "type": "local",
      "command": ["dotnet", "run", "--project", "./SshDiag.Mcp"],
      "enabled": true
    }
  }
}
```

Or add to your global `~/.config/opencode/opencode.json` to make it available everywhere.

### 4. Use

Start opencode in your project folder and ask your AI assistant to diagnose issues:

> "Check why uploads are failing on the WordPress server"

The AI will use `ssh_connect`, `check_php_upload_limits`, `check_permissions`, etc. to diagnose the problem.

## Security

- **Read-only**: Only safe diagnostic commands are allowed. No file writes, service restarts, or configuration changes.
- **Input validation**: `CommandValidator` blocks shell injection characters (`;`, `|`, `&`, `$`, `` ` ``, `>`, `<`) in all user-supplied inputs.
- **Service allowlist**: `service_status` and `journalctl` only accept predefined service names (apache2, nginx, php*-fpm, mysql, etc.).
- **Path validation**: All file paths must be absolute (start with `/`) and cannot contain `..` traversal.
- **Credentials via env vars**: Passwords and keys are read from environment variables, never stored in config files.

## Architecture

```
SshDiag.Mcp/
├── Program.cs              # Entry point, DI setup
├── SshDiag.Mcp.csproj      # Project file
├── Configuration/
│   └── SshDiagConfig.cs    # Env var defaults, output limits
├── Ssh/
│   ├── SshConnectionManager.cs  # SSH connect/disconnect/reconnect
│   └── SshConnectionOptions.cs  # Auth options model
├── Security/
│   ├── CommandValidator.cs      # Input validation & injection prevention
│   └── ReadOnlyExecutor.cs      # Safe command execution with timeout & truncation
└── Tools/
    ├── ConnectionTools.cs       # ssh_connect, ssh_disconnect, ssh_status
    ├── SystemInfoTools.cs       # uname, df, free, uptime, dpkg
    ├── FileSystemTools.cs       # ls, namei, stat, find, file, du, id, cat, head, tail, wc
    ├── TextSearchTools.cs       # grep -r, grep --include
    ├── PhpTools.cs              # php -i, php -m, php -v
    ├── WebServerTools.cs        # apache2ctl -S/-V, nginx -T
    ├── ServiceProcessTools.cs   # systemctl status, ps aux, ss -tlnp
    ├── SecurityModuleTools.cs   # getenforce, sestatus, aa-status
    ├── LogTools.cs              # tail logs, journalctl
    └── CompositeTools.cs        # Multi-command diagnostic combos
```

## License

MIT
