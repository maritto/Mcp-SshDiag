using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SshDiag.Mcp.Configuration;
using SshDiag.Mcp.Ssh;
using SshDiag.Mcp.Security;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton<SshDiagConfig>(sp =>
{
    var config = new SshDiagConfig();

    string? GetEnv(string name) =>
        Environment.GetEnvironmentVariable(name) ??
        Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) ??
        Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);

    var h = GetEnv("SSH_DIAG_HOST"); if (h is not null) config.DefaultHost = h;
    var p = GetEnv("SSH_DIAG_PORT"); if (p is not null && int.TryParse(p, out var port)) config.DefaultPort = port;
    var u = GetEnv("SSH_DIAG_USER"); if (u is not null) config.DefaultUsername = u;
    var pw = GetEnv("SSH_DIAG_PASSWORD"); if (pw is not null) config.DefaultPassword = pw;
    var kp = GetEnv("SSH_DIAG_KEY_PATH"); if (kp is not null) config.DefaultKeyPath = kp;
    var kpp = GetEnv("SSH_DIAG_KEY_PASSPHRASE"); if (kpp is not null) config.DefaultKeyPassphrase = kpp;

    return config;
});
builder.Services.AddSingleton<SshConnectionManager>();
builder.Services.AddSingleton<ReadOnlyExecutor>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
