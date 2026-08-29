using AttendanceApp.Data;

namespace AttendanceApp.Models;

public class AttendanceRecord
{
    public int Id { get; set; }

    public int MeetingSessionId { get; set; }
    public MeetingSession? MeetingSession { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser? Student { get; set; }

    public DateTime CheckedInAt { get; set; }
    public CheckInMethod Method { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    // Lecturer can override the derived status after a query is resolved
    // (e.g. mark present manually even though the student never scanned in).
    public bool ManuallyAdjusted { get; set; }

    // --- Query / dispute thread ---
    public string? QueryMessage { get; set; }
    public bool QueryOpen { get; set; }
    public string? LecturerResponse { get; set; }
}
