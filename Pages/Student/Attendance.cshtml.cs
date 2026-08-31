using AttendanceApp.Data;
using AttendanceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Pages.Student;

[Authorize(Roles = "Student")]
public class AttendanceModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AttendanceModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public record DayStatus(DateOnly Date, bool HasSession, bool Present, bool QueryPending, int? SessionId, string? MeetingTitle);

    public List<DayStatus> Days { get; set; } = new();
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    // All-time, every meeting from every lecturer (no per-module enrolment yet, so a
    // student has no single lecturer to scope against — same "combined pool" convention
    // as the lecturer Overview dashboard; see DESIGN_DECISIONS.md).
    public double MyOverallPercent { get; set; }
    public double ClassAveragePercent { get; set; }
    public int SessionsHeldAllTime { get; set; }

    public async Task OnGetAsync(int? year, int? month)
    {
        var studentId = _userManager.GetUserId(User)!;
        Year = year ?? DateTime.Today.Year;
        Month = month ?? DateTime.Today.Month;

        // Every session across every meeting, since there's no per-module enrollment yet —
        // see the design doc's "open decisions" note if you add course scoping later.
        var sessions = await _db.MeetingSessions
            .Include(s => s.Meeting)
            .Where(s => s.Date.Year == Year && s.Date.Month == Month)
            .ToListAsync();

        var myRecords = await _db.AttendanceRecords
            .Where(a => a.StudentId == studentId)
            .Select(a => new { a.MeetingSessionId, a.Status, a.QueryOpen })
            .ToListAsync();
        var presentIds = myRecords.Where(r => r.Status == AttendanceStatus.Present).Select(r => r.MeetingSessionId).ToHashSet();
        var pendingIds = myRecords.Where(r => r.Status == AttendanceStatus.Disputed && r.QueryOpen).Select(r => r.MeetingSessionId).ToHashSet();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var daysInMonth = DateTime.DaysInMonth(Year, Month);

        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateOnly(Year, Month, d);
            var session = sessions.FirstOrDefault(s => s.Date == date);

            if (session is null)
            {
                Days.Add(new DayStatus(date, false, false, false, null, null));
                continue;
            }

            bool present = presentIds.Contains(session.Id);
            bool pending = pendingIds.Contains(session.Id);
            Days.Add(new DayStatus(date, true, present, pending, session.Id, session.Meeting!.Title));

            if (date <= today)
            {
                if (present) PresentCount++; else AbsentCount++;
            }
        }

        // All-time comparison against the class average — separate from the month-scoped
        // calendar/tiles above, since a single stable "how am I doing overall" number is
        // more meaningful than one that resets every time you flip the calendar page.
        var allHeldSessions = await _db.MeetingSessions.Where(s => s.Date <= today).ToListAsync();
        SessionsHeldAllTime = allHeldSessions.Count;
        var totalStudents = await _db.Users.CountAsync(u => u.StudentNumber != null);

        if (SessionsHeldAllTime > 0 && totalStudents > 0)
        {
            var heldSessionIds = allHeldSessions.Select(s => s.Id).ToHashSet();
            var allPresentRecords = await _db.AttendanceRecords
                .Where(a => heldSessionIds.Contains(a.MeetingSessionId) && a.Status == AttendanceStatus.Present)
                .Select(a => new { a.MeetingSessionId, a.StudentId })
                .ToListAsync();

            int myPresentAllTime = allPresentRecords.Count(r => r.StudentId == studentId);
            MyOverallPercent = Math.Round(100.0 * myPresentAllTime / SessionsHeldAllTime, 1);

            int totalPresentSlots = allPresentRecords.Count;
            ClassAveragePercent = Math.Round(100.0 * totalPresentSlots / (SessionsHeldAllTime * (double)totalStudents), 1);
        }
    }
}
