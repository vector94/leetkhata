using System.Text;
using LeetKhata.Models;

namespace LeetKhata.Services;

public static class ReadmeGenerator
{
    public static string GenerateProblemReadme(
        LeetCodeProblem problem,
        IEnumerable<LanguageSolution> solutions,
        string username)
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

        // One section per language, oldest submission first
        foreach (var solution in solutions.OrderBy(s => s.Timestamp))
        {
            sb.AppendLine($"## {solution.LangVerboseName}");
            sb.AppendLine();

            if (solution.RuntimeDisplay != null)
            {
                var runtimePct = solution.RuntimePercentile.HasValue
                    ? $" (beats {solution.RuntimePercentile.Value:F1}%)"
                    : "";
                sb.AppendLine($"**Runtime:** {solution.RuntimeDisplay}{runtimePct}");
                sb.AppendLine();
            }

            if (solution.MemoryDisplay != null)
            {
                var memoryPct = solution.MemoryPercentile.HasValue
                    ? $" (beats {solution.MemoryPercentile.Value:F1}%)"
                    : "";
                sb.AppendLine($"**Memory:** {solution.MemoryDisplay}{memoryPct}");
                sb.AppendLine();
            }

            var submittedDate = DateTimeOffset.FromUnixTimeSeconds(solution.Timestamp).UtcDateTime;
            sb.AppendLine($"**Submitted:** {submittedDate:MMMM dd, yyyy}");
            sb.AppendLine();
            sb.AppendLine($"**Submission:** [View on LeetCode](https://leetcode.com/problems/{problem.TitleSlug}/submissions/{solution.SubmissionId}/)");
            sb.AppendLine();
        }

        // Attribution
        sb.AppendLine($"**Author:** [{username}](https://leetcode.com/u/{username}/)");
        sb.AppendLine();

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"*Synced by [LeetKhata](https://github.com/mdasifiqbalahmed/LeetKhata) on {DateTime.UtcNow:yyyy-MM-dd}*");

        return sb.ToString();
    }
}
