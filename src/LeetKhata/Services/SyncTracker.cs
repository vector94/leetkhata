using System.Text.Json;
using LeetKhata.Configuration;
using LeetKhata.GitHub;
using LeetKhata.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeetKhata.Services;

public class SyncTracker
{
    private readonly IGitHubSyncClient _github;
    private readonly string _syncStateFilePath;
    private readonly ILogger<SyncTracker> _logger;

    public SyncTracker(IGitHubSyncClient github, IOptions<LeetKhataOptions> options, ILogger<SyncTracker> logger)
    {
        _github = github;
        _syncStateFilePath = options.Value.SyncStateFilePath;
        _logger = logger;
    }

    public async Task<SyncState> LoadStateAsync()
    {
        var content = await _github.GetFileContentAsync(_syncStateFilePath);
        if (content is null)
        {
            _logger.LogInformation("No existing sync state found. Starting fresh.");
            return new SyncState();
        }

        var state = JsonSerializer.Deserialize<SyncState>(content) ?? new SyncState();
        _logger.LogInformation("Loaded sync state with {Count} previously synced submissions.", state.SyncedSubmissionIds.Count);
        return state;
    }

    public string GetSyncStateFilePath() => _syncStateFilePath;

    public string SerializeState(SyncState state)
    {
        state.LastSyncUtc = DateTime.UtcNow;
        return JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
    }
}
