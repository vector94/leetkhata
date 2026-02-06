using System.Text;
using LeetKhata.Models;

namespace LeetKhata.Services;

public static class ReadmeGenerator
{
    public static string GenerateProblemReadme(LeetCodeProblem problem, SubmissionDetail submission, string username)
    {
        var sb = new StringBuilder();

        // Problem info
        sb.AppendLine($"# {problem.QuestionFrontendId}. {problem.Title}");
        sb.AppendLine();
        sb.AppendLine($"**Difficulty:** {problem.Difficulty}");
        sb.AppendLine();
        sb.AppendLine($"**Problem:** [{problem.Title}](https://leetcode.com/problems/{problem.TitleSlug}/)");
        sb.AppendLine();

        if (problem.TopicTags.Count > 0)
        {
            var tags = string.Join(", ", problem.TopicTags.Select(t => t.Name));
            sb.AppendLine($"**Topics:** {tags}");
            sb.AppendLine();
        }

        // Solution details
        sb.AppendLine($"**Language:** {submission.Lang.VerboseName}");
        sb.AppendLine();

        if (submission.RuntimeDisplay != null)
        {
            var runtimePct = submission.RuntimePercentile.HasValue
                ? $" (beats {submission.RuntimePercentile.Value:F1}%)"
                : "";
            sb.AppendLine($"**Runtime:** {submission.RuntimeDisplay}{runtimePct}");
            sb.AppendLine();
        }

        if (submission.MemoryDisplay != null)
        {
            var memoryPct = submission.MemoryPercentile.HasValue
                ? $" (beats {submission.MemoryPercentile.Value:F1}%)"
                : "";
            sb.AppendLine($"**Memory:** {submission.MemoryDisplay}{memoryPct}");
            sb.AppendLine();
        }

        // Attribution
        sb.AppendLine($"**Author:** [{username}](https://leetcode.com/u/{username}/)");
        sb.AppendLine();
        var submittedDate = DateTimeOffset.FromUnixTimeSeconds(submission.Timestamp).UtcDateTime;
        sb.AppendLine($"**Submitted:** {submittedDate:MMMM dd, yyyy}");
        sb.AppendLine();
        sb.AppendLine($"**Submission:** [View on LeetCode](https://leetcode.com/problems/{problem.TitleSlug}/submissions/{submission.Id}/)");
        sb.AppendLine();

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"*Synced by [LeetKhata](https://github.com/mdasifiqbalahmed/LeetKhata) on {DateTime.UtcNow:yyyy-MM-dd}*");

        return sb.ToString();
    }
}
