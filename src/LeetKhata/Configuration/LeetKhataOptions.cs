namespace LeetKhata.Configuration;

public class LeetKhataOptions
{
    public const string SectionName = "LeetKhata";

    // Secrets (from environment variables)
    public string LeetCodeSession { get; set; } = string.Empty;
    public string LeetCodeCsrfToken { get; set; } = string.Empty;
    public string GitHubToken { get; set; } = string.Empty;

    // Non-secrets (from appsettings.json, overridable by env vars)
    public string LeetCodeUsername { get; set; } = string.Empty;
    public string GitHubOwner { get; set; } = string.Empty;
    public string GitHubRepo { get; set; } = "leetcode-solutions";
    public string GitHubBranch { get; set; } = "main";
    public int FetchLimit { get; set; } = 20;
    public string SyncStateFilePath { get; set; } = ".leetkhata/sync-state.json";
}
