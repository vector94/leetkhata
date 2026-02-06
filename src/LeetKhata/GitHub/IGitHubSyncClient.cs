namespace LeetKhata.GitHub;

public interface IGitHubSyncClient
{
    Task<string?> GetFileContentAsync(string path);
    Task CommitFilesAsync(Dictionary<string, string> files, string commitMessage);
}
