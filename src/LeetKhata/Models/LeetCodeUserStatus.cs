using System.Text.Json.Serialization;

namespace LeetKhata.Models;

public record LeetCodeUserStatus(
    [property: JsonPropertyName("isSignedIn")] bool IsSignedIn,
    [property: JsonPropertyName("username")] string? Username
);
