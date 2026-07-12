using LeetKhata.Models;
using LeetKhata.Services;

namespace LeetKhata.Tests;

public class SolutionOrganizerTests
{
    private static readonly LeetCodeProblem Problem = new(
        QuestionId: "1440",
        QuestionFrontendId: "1331",
        Title: "Rank Transform of an Array",
        TitleSlug: "rank-transform-of-an-array",
        Content: null,
        Difficulty: "Easy",
        TopicTags: new List<TopicTag>());

    [Fact]
    public void BuildFilesForSubmission_UsesLanguageSpecificSolutionFile()
    {
        var detail = new SubmissionDetail(
            Id: 22,
            Code: "class Solution: pass",
            Timestamp: 2000,
            RuntimeDisplay: null,
            RuntimePercentile: null,
            MemoryDisplay: null,
            MemoryPercentile: null,
            Lang: new SubmissionLang("python3", "Python3"),
            Question: new SubmissionQuestion("1440", "rank-transform-of-an-array"),
            TopicTags: null);

        var solutions = new List<LanguageSolution>
        {
            new() { LangName = "cpp", LangVerboseName = "C++", SubmissionId = 11, Timestamp = 1000 },
            new() { LangName = "python3", LangVerboseName = "Python3", SubmissionId = 22, Timestamp = 2000 }
        };

        var organizer = new SolutionOrganizer();
        var files = organizer.BuildFilesForSubmission(detail, Problem, solutions, "vector94");

        var basePath = "Easy/1331. Rank Transform of an Array";
        Assert.Contains($"{basePath}/solution.py", files.Keys);
        Assert.Contains($"{basePath}/README.md", files.Keys);
        Assert.Contains("## C++", files[$"{basePath}/README.md"]);
        Assert.Contains("## Python3", files[$"{basePath}/README.md"]);
    }
}
