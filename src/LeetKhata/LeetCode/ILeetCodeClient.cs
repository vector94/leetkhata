using LeetKhata.Models;

namespace LeetKhata.LeetCode;

public interface ILeetCodeClient
{
    Task<List<LeetCodeSubmission>> GetRecentSubmissionsAsync(int offset = 0, int limit = 20);
    Task<SubmissionDetail> GetSubmissionDetailAsync(int submissionId);
    Task<LeetCodeProblem> GetProblemAsync(string titleSlug);
}
