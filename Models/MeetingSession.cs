namespace AttendanceApp.Models;

public class MeetingSession
{
    public int Id { get; set; }

    public int MeetingId { get; set; }
    public Meeting? Meeting { get; set; }

    public DateOnly Date { get; set; }

    // Set when the lecturer clicks "Start meeting". Null/false = not open, no check-ins accepted.
    public bool IsOpen { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Regenerated each time the session is opened, so an old QR code / screenshot can't
    // be reused for a later session of the same recurring meeting.
    public string JoinCode { get; set; } = string.Empty;

    public List<AttendanceRecord> AttendanceRecords { get; set; } = new();
}
