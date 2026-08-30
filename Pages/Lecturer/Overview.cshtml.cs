using AttendanceApp.Data;
using AttendanceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Pages.Lecturer;

// All-time, combined across every meeting this lecturer runs (no course/enrolment
// scoping yet - see DESIGN_DECISIONS.md - so every registered student is treated as
// expected at every held session, same denominator for everyone). Confirmed with the
// developer before building: all-time window, combined scope, band cutoffs
// Excellent >=90 / Good 75-89 / At risk 50-74 / Critical <50, category charts by
// average attendance % rather than raw headcount, and "needs attention" requires
// <75% OR a 2+ session miss streak (a single missed session alone doesn't flag
// someone - tested against a real 104-student sheet, a 1+ threshold flagged 60% of
// the class, too noisy to be useful as an alarm list).
[Authorize(Roles = "Lecturer")]
public class OverviewModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public OverviewModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // ---- Snapshot tiles ----
    public double OverallPercent { get; set; }
    public int SessionsHeld { get; set; }
    public int StudentsTracked { get; set; }
    public int NeedAttentionCount { get; set; }

    // ---- Students who need attention ----
    public record AttentionRow(string Name, string StudentNumber, double Percent, int MissedStreak, DateOnly? LastAttended, bool Critical);
    public List<AttentionRow> NeedAttention { get; set; } = new();

    // ---- Trend (weekly) ----
    public record WeekPoint(string Label, double Percent);
    public List<WeekPoint> Trend { get; set; } = new();

    // ---- Distribution donut ----
    public int ExcellentCount { get; set; }
    public int GoodCount { get; set; }
    public int AtRiskCount { get; set; }
    public int CriticalCount { get; set; }

    // ---- By session type / by day of week ----
    public record CategoryPoint(string Label, double Percent);
    public List<CategoryPoint> BySessionType { get; set; } = new();
    public List<CategoryPoint> ByDayOfWeek { get; set; } = new();

    // ---- Per-student table ----
    public record StudentHistoryEntry(DateOnly Date, string MeetingTitle, bool Present);
    public record StudentRow(string Name, string StudentNumber, double Percent, int Present, int Total, List<StudentHistoryEntry> History);
    public List<StudentRow> Students { get; set; } = new();

    public async Task OnGetAsync()
    {
        var lecturerId = _userManager.GetUserId(User);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var heldSessions = await _db.MeetingSessions
            .Include(s => s.Meeting)
            .Where(s => s.Meeting!.LecturerId == lecturerId && s.Date <= today)
            .OrderBy(s => s.Date)
            .ToListAsync();

        SessionsHeld = heldSessions.Count;
        var heldSessionIds = heldSessions.Select(s => s.Id).ToHashSet();

        var students = await _db.Users
            .Where(u => u.StudentNumber != null)
            .OrderBy(u => u.FullName)
            .ToListAsync();
        StudentsTracked = students.Count;

        if (SessionsHeld == 0 || StudentsTracked == 0)
        {
            return; // nothing to compute yet - views handle empty state
        }

        var presentRecords = await _db.AttendanceRecords
            .Where(a => heldSessionIds.Contains(a.MeetingSessionId) && a.Status == AttendanceStatus.Present)
            .Select(a => new { a.MeetingSessionId, a.StudentId })
            .ToListAsync();

        var presentSessionIdsByStudent = presentRecords
            .GroupBy(r => r.StudentId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.MeetingSessionId).ToHashSet());

        int totalPresentSlots = presentRecords.Count;
        OverallPercent = Math.Round(100.0 * totalPresentSlots / (SessionsHeld * (double)StudentsTracked), 1);

        // Sessions newest-first, needed for streaks/last-attended and reused for the table.
        var sessionsNewestFirst = heldSessions.AsEnumerable().Reverse().ToList();

        foreach (var student in students)
        {
            presentSessionIdsByStudent.TryGetValue(student.Id, out var presentIds);
            presentIds ??= new HashSet<int>();

            int present = presentIds.Count;
            double percent = Math.Round(100.0 * present / SessionsHeld, 1);

            int missedStreak = 0;
            DateOnly? lastAttended = null;
            foreach (var s in sessionsNewestFirst)
            {
                if (presentIds.Contains(s.Id))
                {
                    lastAttended = s.Date;
                    break;
                }
                missedStreak++;
            }

            bool critical = percent < 50 || missedStreak >= 3;
            bool flagged = percent < 75 || missedStreak >= 2;

            if (flagged)
            {
                NeedAttention.Add(new AttentionRow(student.FullName, student.StudentNumber!, percent, missedStreak, lastAttended, critical));
            }

            if (percent >= 90) ExcellentCount++;
            else if (percent >= 75) GoodCount++;
            else if (percent >= 50) AtRiskCount++;
            else CriticalCount++;

            var history = heldSessions
                .Select(s => new StudentHistoryEntry(s.Date, s.Meeting!.Title, presentIds.Contains(s.Id)))
                .OrderByDescending(h => h.Date)
                .ToList();

            Students.Add(new StudentRow(student.FullName, student.StudentNumber!, percent, present, SessionsHeld, history));
        }

        NeedAttentionCount = NeedAttention.Count;
        NeedAttention = NeedAttention
            .OrderByDescending(r => r.Critical)
            .ThenBy(r => r.Percent)
            .ToList();

        Students = Students.OrderBy(s => s.Percent).ToList();

        // Weekly trend: bucket by the Monday of each session's week.
        Trend = heldSessions
            .GroupBy(s => s.Date.AddDays(-(int)s.Date.DayOfWeek + (s.Date.DayOfWeek == DayOfWeek.Sunday ? -6 : 1)))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var sessionIds = g.Select(s => s.Id).ToHashSet();
                int presentInWeek = presentRecords.Count(r => sessionIds.Contains(r.MeetingSessionId));
                double pct = Math.Round(100.0 * presentInWeek / (sessionIds.Count * (double)StudentsTracked), 1);
                return new WeekPoint(g.Key.ToString("d MMM"), pct);
            })
            .ToList();

        BySessionType = heldSessions
            .GroupBy(s => s.Meeting!.Type)
            .Select(g =>
            {
                var sessionIds = g.Select(s => s.Id).ToHashSet();
                int presentInGroup = presentRecords.Count(r => sessionIds.Contains(r.MeetingSessionId));
                double pct = Math.Round(100.0 * presentInGroup / (sessionIds.Count * (double)StudentsTracked), 1);
                return new CategoryPoint(g.Key.ToString(), pct);
            })
            .ToList();

        ByDayOfWeek = heldSessions
            .GroupBy(s => s.Date.DayOfWeek)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var sessionIds = g.Select(s => s.Id).ToHashSet();
                int presentInGroup = presentRecords.Count(r => sessionIds.Contains(r.MeetingSessionId));
                double pct = Math.Round(100.0 * presentInGroup / (sessionIds.Count * (double)StudentsTracked), 1);
                return new CategoryPoint(g.Key.ToString(), pct);
            })
            .ToList();
    }
}
