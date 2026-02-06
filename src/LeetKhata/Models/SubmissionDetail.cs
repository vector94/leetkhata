using System.Text.Json.Serialization;

namespace LeetKhata.Models;

public record SubmissionDetail(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("timestamp")] long Timestamp,
    [property: JsonPropertyName("runtimeDisplay")] string? RuntimeDisplay,
    [property: JsonPropertyName("runtimePercentile")] double? RuntimePercentile,
    [property: JsonPropertyName("memoryDisplay")] string? MemoryDisplay,
    [property: JsonPropertyName("memoryPercentile")] double? MemoryPercentile,
    [property: JsonPropertyName("lang")] SubmissionLang Lang,
    [property: JsonPropertyName("question")] SubmissionQuestion Question,
    [property: JsonPropertyName("topicTags")] List<TopicTag>? TopicTags
);

public record SubmissionLang(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("verboseName")] string VerboseName
);

public record SubmissionQuestion(
    [property: JsonPropertyName("questionId")] string QuestionId,
    [property: JsonPropertyName("titleSlug")] string TitleSlug
);
