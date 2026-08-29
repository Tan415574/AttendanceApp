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
    }
}
