using System.Text.Json.Serialization;

namespace LeetKhata.Models;

public record LeetCodeSubmission(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("titleSlug")] string TitleSlug,
    [property: JsonPropertyName("timestamp")] string Timestamp,
    [property: JsonPropertyName("statusDisplay")] string StatusDisplay,
    [property: JsonPropertyName("lang")] string Lang,
    [property: JsonPropertyName("runtime")] string? Runtime,
    [property: JsonPropertyName("memory")] string? Memory
);
