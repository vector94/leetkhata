using LeetKhata.Configuration;
using LeetKhata.GitHub;
using LeetKhata.LeetCode;
using LeetKhata.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

// Load .env file from working directory if it exists (written by scripts/refresh-cookies.py)
var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
var envVars = new Dictionary<string, string?>();
if (File.Exists(envFile))
{
    foreach (var line in File.ReadAllLines(envFile))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            continue;
        var idx = trimmed.IndexOf('=');
        if (idx <= 0)
            continue;
        var key = trimmed[..idx].Trim().Replace("__", ":");
        var value = trimmed[(idx + 1)..].Trim();
        envVars[key] = value;
    }
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddInMemoryCollection(envVars)
    .AddEnvironmentVariables("LEETKHATA__")
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .WriteTo.Console()
    .CreateLogger();

var services = new ServiceCollection();

services.AddLogging(builder => builder.AddSerilog(dispose: true));
// Env vars with LEETKHATA__ prefix map directly (prefix is stripped).
// appsettings.json uses the "LeetKhata" section.
// Merge both: section values first, then env var overrides.
var section = configuration.GetSection(LeetKhataOptions.SectionName);
services.Configure<LeetKhataOptions>(opts =>
{
    section.Bind(opts);
    configuration.Bind(opts);
});

services.AddHttpClient("LeetCode", (sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<LeetKhataOptions>>().Value;
    client.BaseAddress = new Uri("https://leetcode.com");
    client.DefaultRequestHeaders.Add("Cookie",
        $"LEETCODE_SESSION={opts.LeetCodeSession}; csrftoken={opts.LeetCodeCsrfToken}");
    client.DefaultRequestHeaders.Add("x-csrftoken", opts.LeetCodeCsrfToken);
    client.DefaultRequestHeaders.Add("Referer", "https://leetcode.com");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("LeetKhata/1.0");
});

services.AddSingleton<ILeetCodeClient, LeetCodeClient>();
services.AddSingleton<IGitHubSyncClient>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<LeetKhataOptions>>().Value;
    var logger = sp.GetRequiredService<ILogger<GitHubSyncClient>>();
    return new GitHubSyncClient(opts.GitHubToken, opts.GitHubOwner, opts.GitHubRepo, opts.GitHubBranch, logger);
});
services.AddSingleton<SolutionOrganizer>();
services.AddSingleton<SyncTracker>();
services.AddSingleton<SyncOrchestrator>();

var provider = services.BuildServiceProvider();

try
{
    // Validate required configuration before running
    var opts = provider.GetRequiredService<IOptions<LeetKhataOptions>>().Value;
    var missing = new List<string>();

    if (string.IsNullOrWhiteSpace(opts.LeetCodeSession))
        missing.Add("LEETKHATA__LeetCodeSession");
    if (string.IsNullOrWhiteSpace(opts.LeetCodeCsrfToken))
        missing.Add("LEETKHATA__LeetCodeCsrfToken");
    if (string.IsNullOrWhiteSpace(opts.GitHubToken))
        missing.Add("LEETKHATA__GitHubToken");
    if (string.IsNullOrWhiteSpace(opts.LeetCodeUsername))
        missing.Add("LEETKHATA__LeetCodeUsername");
    if (string.IsNullOrWhiteSpace(opts.GitHubOwner))
        missing.Add("LEETKHATA__GitHubOwner");
    if (string.IsNullOrWhiteSpace(opts.GitHubRepo))
        missing.Add("LEETKHATA__GitHubRepo");

    if (missing.Count > 0)
    {
        Log.Error("Missing required configuration. Set these environment variables:\n  - {Variables}",
            string.Join("\n  - ", missing));
        return 1;
    }

    var orchestrator = provider.GetRequiredService<SyncOrchestrator>();
    await orchestrator.RunAsync();
    return 0;
}
catch (LeetCodeSessionExpiredException ex)
{
    Log.Error(ex.Message);
    return 1;
}
catch (Exception ex)
{
    Log.Fatal(ex, "LeetKhata failed.");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
