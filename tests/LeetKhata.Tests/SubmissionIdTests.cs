using System.Text.Json;
using LeetKhata.Models;

namespace LeetKhata.Tests;

// LeetCode submission IDs have grown past int.MaxValue (2,147,483,647).
public class SubmissionIdTests
{
    private const long LargeId = 2181589653;

    [Fact]
    public void SubmissionDetail_DeserializesIdAboveInt32Max()
    {
        var json = $$"""
            {
              "id": {{LargeId}},
              "code": "class Solution: pass",
              "timestamp": 1761400000,
              "runtimeDisplay": "3 ms",
              "runtimePercentile": 91.2,
              "memoryDisplay": "17.9 MB",
              "memoryPercentile": 55.0,
              "lang": { "name": "python3", "verboseName": "Python3" },
              "question": { "questionId": "1380", "titleSlug": "number-of-closed-islands" },
              "topicTags": []
            }
            """;

        var detail = JsonSerializer.Deserialize<SubmissionDetail>(json);

        Assert.Equal(LargeId, detail!.Id);
    }

    [Fact]
    public void SyncState_RoundTripsSubmissionIdAboveInt32Max()
    {
        var state = new SyncState();
        state.ProblemSolutions["number-of-closed-islands"] = new List<LanguageSolution>
        {
            new() { LangName = "python3", LangVerboseName = "Python3", SubmissionId = LargeId }
        };

        var json = JsonSerializer.Serialize(state);
        var loaded = JsonSerializer.Deserialize<SyncState>(json);

        Assert.Equal(LargeId, loaded!.ProblemSolutions["number-of-closed-islands"][0].SubmissionId);
    }

    [Fact]
    public void SyncState_LoadsExistingStateWithSmallSubmissionIds()
    {
        const string json = """
            {
              "SyncedSubmissionIds": ["1234567890"],
              "ProblemSolutions": {
                "two-sum": [{ "LangName": "java", "LangVerboseName": "Java", "SubmissionId": 1234567890, "Timestamp": 1700000000 }]
              }
            }
            """;

        var loaded = JsonSerializer.Deserialize<SyncState>(json);

        Assert.Equal(1234567890, loaded!.ProblemSolutions["two-sum"][0].SubmissionId);
    }
}
