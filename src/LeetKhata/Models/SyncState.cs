namespace LeetKhata.Models;

public class SyncState
{
    public HashSet<string> SyncedSubmissionIds { get; set; } = new();
    public DateTime LastSyncUtc { get; set; } = DateTime.MinValue;

    // Keyed by problem title slug; one entry per language so the problem README
    // can be regenerated with every language's stats, even across sync runs.
    public Dictionary<string, List<LanguageSolution>> ProblemSolutions { get; set; } = new();
}

public class LanguageSolution
{
    public string LangName { get; set; } = "";
    public string LangVerboseName { get; set; } = "";
    public int SubmissionId { get; set; }
    public long Timestamp { get; set; }
    public string? RuntimeDisplay { get; set; }
    public double? RuntimePercentile { get; set; }
    public string? MemoryDisplay { get; set; }
    public double? MemoryPercentile { get; set; }
}
