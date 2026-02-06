using LeetKhata.Models;

namespace LeetKhata.Services;

public class SolutionOrganizer
{
    public Dictionary<string, string> BuildFilesForSubmission(
        SubmissionDetail submission,
        LeetCodeProblem problem,
        string username)
    {
        var files = new Dictionary<string, string>();
        var folderName = BuildFolderName(problem);
        var basePath = $"{problem.Difficulty}/{folderName}";
        var extension = GetFileExtension(submission.Lang.Name);

        files[$"{basePath}/solution{extension}"] = submission.Code;
        files[$"{basePath}/README.md"] = ReadmeGenerator.GenerateProblemReadme(problem, submission, username);

        return files;
    }

    private static string BuildFolderName(LeetCodeProblem problem)
    {
        return $"{problem.QuestionFrontendId}. {problem.Title}";
    }

    public static string GetFileExtension(string langName) => langName.ToLower() switch
    {
        "c#" or "csharp" => ".cs",
        "python" or "python3" => ".py",
        "java" => ".java",
        "javascript" => ".js",
        "typescript" => ".ts",
        "c++" or "cpp" => ".cpp",
        "c" => ".c",
        "go" or "golang" => ".go",
        "rust" => ".rs",
        "ruby" => ".rb",
        "swift" => ".swift",
        "kotlin" => ".kt",
        "scala" => ".scala",
        "php" => ".php",
        "dart" => ".dart",
        "sql" or "mysql" or "mssql" or "oraclesql" => ".sql",
        _ => ".txt"
    };
}
