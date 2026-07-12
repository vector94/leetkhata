using LeetKhata.Configuration;
using LeetKhata.GitHub;
using LeetKhata.LeetCode;
using LeetKhata.Models;
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

        // 2. Fetch submissions from LeetCode (paginated, 20 per request)
        var allSubmissions = new List<LeetCodeSubmission>();
        const int pageSize = 20;
        var offset = 0;

        while (offset < _options.FetchLimit)
        {
            var batch = await _leetcode.GetRecentSubmissionsAsync(offset, pageSize);
            if (batch.Count == 0)
                break;

            allSubmissions.AddRange(batch);
            offset += batch.Count;

            if (batch.Count < pageSize)
                break;
        }

        var accepted = allSubmissions
            .Where(s => s.StatusDisplay == "Accepted")
            .ToList();

        _logger.LogInformation("Found {Count} accepted submissions out of {Total} total.",
            accepted.Count, allSubmissions.Count);

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

                if (!state.ProblemSolutions.TryGetValue(problem.TitleSlug, out var solutions))
                {
                    solutions = new List<LanguageSolution>();
                    state.ProblemSolutions[problem.TitleSlug] = solutions;
                }

                var entry = new LanguageSolution
                {
                    LangName = detail.Lang.Name,
                    LangVerboseName = detail.Lang.VerboseName,
                    SubmissionId = detail.Id,
                    Timestamp = detail.Timestamp,
                    RuntimeDisplay = detail.RuntimeDisplay,
                    RuntimePercentile = detail.RuntimePercentile,
                    MemoryDisplay = detail.MemoryDisplay,
                    MemoryPercentile = detail.MemoryPercentile
                };

                // Keep only the newest submission per language; submissions arrive
                // newest-first, so an older duplicate must not overwrite a newer one.
                var existing = solutions.FirstOrDefault(s => s.LangName == entry.LangName);
                if (existing is null || entry.Timestamp >= existing.Timestamp)
                {
                    if (existing is not null)
                        solutions.Remove(existing);
                    solutions.Add(entry);

                    var files = _organizer.BuildFilesForSubmission(detail, problem, solutions, _options.LeetCodeUsername);
                    foreach (var kvp in files)
                    {
                        allFiles[kvp.Key] = kvp.Value;
                    }

                    _logger.LogInformation("Prepared: {Title} ({Lang}) -> {Difficulty}/{Slug}",
                        problem.Title, detail.Lang.VerboseName, problem.Difficulty, problem.TitleSlug);
                }
                else
                {
                    _logger.LogInformation(
                        "Skipping older {Lang} submission for '{Title}'; a newer one is already synced.",
                        detail.Lang.VerboseName, problem.Title);
                }

                syncedIds.Add(sub.Id);
                if (!syncedProblems.Any(p => p.Id == problem.QuestionFrontendId))
                    syncedProblems.Add((problem.QuestionFrontendId, problem.Title, problem.Difficulty));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process submission '{Title}' (ID: {Id}). Skipping.",
                    sub.Title, sub.Id);
            }
        }

        if (syncedIds.Count == 0)
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

        // 6. Update repo README with difficulty counts. Union existing repo folders
        // with the ones in this batch so a new language for an already-synced
        // problem doesn't double-count it.
        var folders = await _github.GetProblemFoldersAsync();
        var counts = new Dictionary<string, int> { ["Easy"] = 0, ["Medium"] = 0, ["Hard"] = 0 };
        foreach (var path in allFiles.Keys)
        {
            var parts = path.Split('/');
            if (parts.Length >= 3 && counts.ContainsKey(parts[0]))
                folders.Add($"{parts[0]}/{parts[1]}");
        }
        foreach (var folder in folders)
        {
            counts[folder.Split('/')[0]]++;
        }
        allFiles["README.md"] = BuildReadme(counts["Easy"], counts["Medium"], counts["Hard"]);

        // 7. Commit all files in a single commit
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

    private static string BuildReadme(int easy, int medium, int hard) =>
$@"# LeetCode Solutions

This repository contains all my accepted LeetCode submissions, organized by difficulty.

| Difficulty | Count |
|------------|-------|
| Easy       | {easy} |
| Medium     | {medium} |
| Hard       | {hard} |

Each solution includes the code and a README with problem metadata (topics, runtime, memory, submission link).

## Sync

Solutions are automatically synced from LeetCode using [LeetKhata](https://github.com/vector94/leetkhata), a tool that fetches accepted submissions via the LeetCode API and pushes them to GitHub, organized by difficulty.
";
}
