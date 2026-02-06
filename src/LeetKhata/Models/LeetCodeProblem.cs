using System.Text.Json.Serialization;

namespace LeetKhata.Models;

public record LeetCodeProblem(
    [property: JsonPropertyName("questionId")] string QuestionId,
    [property: JsonPropertyName("questionFrontendId")] string QuestionFrontendId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("titleSlug")] string TitleSlug,
    [property: JsonPropertyName("content")] string? Content,
    [property: JsonPropertyName("difficulty")] string Difficulty,
    [property: JsonPropertyName("topicTags")] List<TopicTag> TopicTags
);

public record TopicTag(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug
);
