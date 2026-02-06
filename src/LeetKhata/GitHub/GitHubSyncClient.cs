using Microsoft.Extensions.Logging;
using Octokit;

namespace LeetKhata.GitHub;

public class GitHubSyncClient : IGitHubSyncClient
{
    private readonly GitHubClient _client;
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _branch;
    private readonly ILogger<GitHubSyncClient> _logger;

    public GitHubSyncClient(string token, string owner, string repo, string branch, ILogger<GitHubSyncClient> logger)
    {
        _client = new GitHubClient(new ProductHeaderValue("LeetKhata"))
        {
            Credentials = new Credentials(token)
        };
        _owner = owner;
        _repo = repo;
        _branch = branch;
        _logger = logger;
    }

    public async Task<string?> GetFileContentAsync(string path)
    {
        try
        {
            var contents = await _client.Repository.Content.GetAllContentsByRef(_owner, _repo, path, _branch);
            if (contents.Count > 0 && contents[0].Content != null)
            {
                return contents[0].Content;
            }

            // Content might be too large and returned as base64
            if (contents.Count > 0 && contents[0].EncodedContent != null)
            {
                var bytes = Convert.FromBase64String(contents[0].EncodedContent);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }

            return null;
        }
        catch (NotFoundException)
        {
            _logger.LogDebug("File not found in repo: {Path}", path);
            return null;
        }
    }

    public async Task CommitFilesAsync(Dictionary<string, string> files, string commitMessage)
    {
        _logger.LogInformation("Committing {Count} files to {Owner}/{Repo}@{Branch}...",
            files.Count, _owner, _repo, _branch);

        // 1. Get the reference to HEAD of the branch
        var reference = await _client.Git.Reference.Get(_owner, _repo, $"heads/{_branch}");
        var latestCommitSha = reference.Object.Sha;
        var latestCommit = await _client.Git.Commit.Get(_owner, _repo, latestCommitSha);

        // 2. Create blobs for each file
        var treeItems = new List<NewTreeItem>();
        foreach (var (path, content) in files)
        {
            var blob = await _client.Git.Blob.Create(_owner, _repo, new NewBlob
            {
                Content = content,
                Encoding = EncodingType.Utf8
            });

            treeItems.Add(new NewTreeItem
            {
                Path = path,
                Mode = "100644",
                Type = TreeType.Blob,
                Sha = blob.Sha
            });

            _logger.LogDebug("Created blob for {Path}", path);
        }

        // 3. Create a new tree based on the current tree
        var newTree = new NewTree { BaseTree = latestCommit.Tree.Sha };
        foreach (var item in treeItems)
        {
            newTree.Tree.Add(item);
        }

        var tree = await _client.Git.Tree.Create(_owner, _repo, newTree);

        // 4. Create the commit
        var newCommit = new NewCommit(commitMessage, tree.Sha, latestCommitSha);
        var commit = await _client.Git.Commit.Create(_owner, _repo, newCommit);

        // 5. Update the branch reference to point to the new commit
        await _client.Git.Reference.Update(_owner, _repo, $"heads/{_branch}",
            new ReferenceUpdate(commit.Sha));

        _logger.LogInformation("Committed successfully: {Sha} - {Message}", commit.Sha[..7], commitMessage);
    }
}
