using AttendanceApp.Data;

namespace AttendanceApp.Models;

// A Meeting is the *template* the lecturer defines: "Tuesday Workshop, weekly, 14:00".
// Each real occurrence (this Tuesday, next Tuesday...) is a MeetingSession, generated
// from this template. This split is what lets "start a meeting" mean "open today's
// occurrence for check-in" rather than re-creating the whole thing every week.
public class Meeting
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;          // short heading, e.g. "Databases Workshop 3"
    public string? Description { get; set; }                   // optional longer blurb

    public MeetingType Type { get; set; }
    public RecurrencePattern Recurrence { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }                      // required if Weekly; null if OnceOff
    public DayOfWeek? DayOfWeek { get; set; }                   // required if Weekly
    public TimeOnly TimeOfDay { get; set; }

    public string LecturerId { get; set; } = string.Empty;
    public ApplicationUser? Lecturer { get; set; }

    public List<MeetingSession> Sessions { get; set; } = new();

    // Expands this template into concrete calendar dates. Called once at creation
    // to materialize MeetingSession rows, and re-run if the lecturer edits dates.
    public IEnumerable<DateOnly> GetOccurrenceDates()
    {
        if (Recurrence == RecurrencePattern.OnceOff)
        {
            yield return StartDate;
            yield break;
        }

        if (DayOfWeek is null || EndDate is null)
            yield break; // caller should validate before this is reached

        var date = StartDate;
        // Move forward to the first matching weekday on/after StartDate
        while (date.DayOfWeek != DayOfWeek.Value)
            date = date.AddDays(1);

        while (date <= EndDate.Value)
        {
            yield return date;
            date = date.AddDays(7);
        }
    }
}
