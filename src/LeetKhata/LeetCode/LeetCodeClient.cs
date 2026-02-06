using System.Text.Json;
using LeetKhata.Models;
using Microsoft.Extensions.Logging;

namespace LeetKhata.LeetCode;

public class LeetCodeClient : ILeetCodeClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LeetCodeClient> _logger;

    public LeetCodeClient(IHttpClientFactory httpClientFactory, ILogger<LeetCodeClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<LeetCodeSubmission>> GetRecentSubmissionsAsync(int offset = 0, int limit = 20)
    {
        _logger.LogInformation("Fetching submissions (offset={Offset}, limit={Limit})...", offset, limit);

        var response = await SendGraphQLAsync(Queries.SubmissionList, new
        {
            offset,
            limit,
            slug = (string?)null
        });

        var submissionList = response.RootElement
            .GetProperty("data")
            .GetProperty("submissionList")
            .GetProperty("submissions");

        var submissions = JsonSerializer.Deserialize<List<LeetCodeSubmission>>(submissionList.GetRawText())
            ?? new List<LeetCodeSubmission>();

        _logger.LogInformation("Fetched {Count} submissions.", submissions.Count);
        return submissions;
    }

    public async Task<SubmissionDetail> GetSubmissionDetailAsync(int submissionId)
    {
        _logger.LogInformation("Fetching submission detail for ID {Id}...", submissionId);

        var response = await SendGraphQLAsync(Queries.SubmissionDetails, new { id = submissionId });

        var detailJson = response.RootElement
            .GetProperty("data")
            .GetProperty("submissionDetails");

        var detail = JsonSerializer.Deserialize<SubmissionDetail>(detailJson.GetRawText())
            ?? throw new InvalidOperationException($"Failed to deserialize submission detail for ID {submissionId}");

        return detail;
    }

    public async Task<LeetCodeProblem> GetProblemAsync(string titleSlug)
    {
        _logger.LogInformation("Fetching problem data for '{Slug}'...", titleSlug);

        var response = await SendGraphQLAsync(Queries.QuestionData, new { titleSlug });

        var questionJson = response.RootElement
            .GetProperty("data")
            .GetProperty("question");

        var problem = JsonSerializer.Deserialize<LeetCodeProblem>(questionJson.GetRawText())
            ?? throw new InvalidOperationException($"Failed to deserialize problem '{titleSlug}'");

        return problem;
    }

    private async Task<JsonDocument> SendGraphQLAsync(string query, object variables)
    {
        var client = _httpClientFactory.CreateClient("LeetCode");

        var requestBody = new
        {
            query,
            variables
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/graphql/", content);

        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
            or System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogError("LeetCode returned {StatusCode} — session cookies are likely expired.", response.StatusCode);
            throw new LeetCodeSessionExpiredException();
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("LeetCode API error {StatusCode}: {Body}", response.StatusCode, errorBody);
            throw new HttpRequestException($"LeetCode API returned {response.StatusCode}: {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        // LeetCode sometimes returns 200 with errors in the body when session is invalid
        if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
        {
            var errorMsg = errors[0].TryGetProperty("message", out var msg)
                ? msg.GetString() ?? "Unknown error"
                : "Unknown error";

            if (errorMsg.Contains("sign in", StringComparison.OrdinalIgnoreCase)
                || errorMsg.Contains("login", StringComparison.OrdinalIgnoreCase)
                || errorMsg.Contains("authenticated", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("LeetCode API requires authentication: {Message}", errorMsg);
                throw new LeetCodeSessionExpiredException();
            }

            _logger.LogError("LeetCode GraphQL error: {Message}", errorMsg);
            throw new InvalidOperationException($"LeetCode GraphQL error: {errorMsg}");
        }

        return doc;
    }
}
