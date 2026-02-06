namespace LeetKhata.Models;

public class SyncState
{
    public HashSet<string> SyncedSubmissionIds { get; set; } = new();
    public DateTime LastSyncUtc { get; set; } = DateTime.MinValue;
}
