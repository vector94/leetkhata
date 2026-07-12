using LeetKhata.Models;
using LeetKhata.Services;

namespace LeetKhata.Tests;

public class ReadmeGeneratorTests
{
    private static readonly LeetCodeProblem Problem = new(
        QuestionId: "1440",
        QuestionFrontendId: "1331",
        Title: "Rank Transform of an Array",
        TitleSlug: "rank-transform-of-an-array",
        Content: null,
        Difficulty: "Easy",
        TopicTags: new List<TopicTag>
        {
            new("Array", "array"),
            new("Hash Table", "hash-table"),
            new("Sorting", "sorting")
        });

    private static LanguageSolution Solution(string lang, string verbose, long timestamp, int id = 1) => new()
    {
        LangName = lang,
        LangVerboseName = verbose,
        SubmissionId = id,
        Timestamp = timestamp,
        RuntimeDisplay = "17 ms",
        RuntimePercentile = 98.7,
        MemoryDisplay = "39.8 MB",
        MemoryPercentile = 83.4
    };

    [Fact]
    public void GenerateProblemReadme_IncludesSectionPerLanguage()
    {
        var solutions = new[]
        {
            Solution("cpp", "C++", 1000, id: 11),
            Solution("python3", "Python3", 2000, id: 22)
        };

        var readme = ReadmeGenerator.GenerateProblemReadme(Problem, solutions, "vector94");

        Assert.Contains("## C++", readme);
        Assert.Contains("## Python3", readme);
        Assert.Contains("submissions/11/", readme);
        Assert.Contains("submissions/22/", readme);
    }

    [Fact]
    public void GenerateProblemReadme_OrdersLanguagesBySubmissionTime()
    {
        var solutions = new[]
        {
            Solution("python3", "Python3", 2000),
            Solution("cpp", "C++", 1000)
        };

        var readme = ReadmeGenerator.GenerateProblemReadme(Problem, solutions, "vector94");

        Assert.True(readme.IndexOf("## C++") < readme.IndexOf("## Python3"),
            "the older submission's section should come first");
    }

    [Fact]
    public void GenerateProblemReadme_IncludesProblemMetadataOnce()
    {
        var solutions = new[]
        {
            Solution("cpp", "C++", 1000),
            Solution("python3", "Python3", 2000)
        };

        var readme = ReadmeGenerator.GenerateProblemReadme(Problem, solutions, "vector94");

        Assert.Contains("# 1331. Rank Transform of an Array", readme);
        Assert.Contains("**Difficulty:** Easy", readme);
        Assert.Contains("**Topics:** Array, Hash Table, Sorting", readme);
        Assert.Single(SplitOccurrences(readme, "**Author:**"));
    }

    private static IEnumerable<int> SplitOccurrences(string text, string token)
    {
        for (var i = text.IndexOf(token); i >= 0; i = text.IndexOf(token, i + 1))
            yield return i;
    }
}
