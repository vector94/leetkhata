using LeetKhata.Configuration;
using LeetKhata.GitHub;
using LeetKhata.LeetCode;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeetKhata.Services;

public class SyncOrchestrator
{
    private readonly ILeetCodeClient _leetcode;
    private readonly IGitHubSyncClient _github;
    private readonly SolutionOrganizer _organizer;
    private readonly SyncTracker _tracker;
    private readonly LeetKhataOptions _options;
    private readonly ILogger<SyncOrchestrator> _logger;

    public SyncOrchestrator(
        ILeetCodeClient leetcode,
        IGitHubSyncClient github,
        SolutionOrganizer organizer,
        SyncTracker tracker,
        IOptions<LeetKhataOptions> options,
        ILogger<SyncOrchestrator> logger)
    {
        _leetcode = leetcode;
        _github = github;
        _organizer = organizer;
        _tracker = tracker;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("Starting LeetKhata sync...");

        // 1. Load sync state from the repo
        var state = await _tracker.LoadStateAsync();

        // 2. Fetch recent submissions from LeetCode
        var submissions = await _leetcode.GetRecentSubmissionsAsync(offset: 0, limit: _options.FetchLimit);
        var accepted = submissions
            .Where(s => s.StatusDisplay == "Accepted")
            .ToList();

        _logger.LogInformation("Found {Count} accepted submissions out of {Total} total.",
            accepted.Count, submissions.Count);

        // 3. Filter to only new (unsynced) submissions
        var newSubmissions = accepted
            .Where(s => !state.SyncedSubmissionIds.Contains(s.Id))
            .ToList();

        if (newSubmissions.Count == 0)
        {
            _logger.LogInformation("No new submissions to sync. Everything is up to date.");
            return;
        }

        _logger.LogInformation("{Count} new submission(s) to sync.", newSubmissions.Count);

        // 4. For each new submission, fetch details and problem data
        var allFiles = new Dictionary<string, string>();
        var syncedIds = new List<string>();
        var syncedProblems = new List<(string Id, string Title, string Difficulty)>();

        foreach (var sub in newSubmissions)
        {
            try
            {
                _logger.LogInformation("Processing: {Title} ({Lang})", sub.Title, sub.Lang);

                // Rate limiting: wait between API calls
                await Task.Delay(1000);
                var detail = await _leetcode.GetSubmissionDetailAsync(int.Parse(sub.Id));

                await Task.Delay(1000);
                var problem = await _leetcode.GetProblemAsync(sub.TitleSlug);

                var files = _organizer.BuildFilesForSubmission(detail, problem, _options.LeetCodeUsername);
                foreach (var kvp in files)
                {
                    allFiles[kvp.Key] = kvp.Value;
                }

                syncedIds.Add(sub.Id);
                syncedProblems.Add((problem.QuestionFrontendId, problem.Title, problem.Difficulty));
                _logger.LogInformation("Prepared: {Title} -> {Difficulty}/{Slug}",
                    problem.Title, problem.Difficulty, problem.TitleSlug);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process submission '{Title}' (ID: {Id}). Skipping.",
                    sub.Title, sub.Id);
            }
        }

        if (allFiles.Count == 0)
        {
            _logger.LogWarning("No files to commit after processing. All submissions may have failed.");
            return;
        }

        // 5. Update sync state
        foreach (var id in syncedIds)
        {
            state.SyncedSubmissionIds.Add(id);
        }
        allFiles[_tracker.GetSyncStateFilePath()] = _tracker.SerializeState(state);

        // 6. Commit all files in a single commit
        var sb = new System.Text.StringBuilder();
        if (syncedProblems.Count == 1)
            sb.Append($"LeetKhata: Add {syncedProblems[0].Id}. {syncedProblems[0].Title} ({syncedProblems[0].Difficulty})");
        else
        {
            sb.AppendLine($"LeetKhata: Add {syncedProblems.Count} solutions");
            sb.AppendLine();
            foreach (var (id, title, difficulty) in syncedProblems)
                sb.AppendLine($"- {id}. {title} ({difficulty})");
        }
        var message = sb.ToString().TrimEnd();

        await _github.CommitFilesAsync(allFiles, message);

        _logger.LogInformation("Successfully synced {Count} submission(s).", syncedIds.Count);
    }
}
