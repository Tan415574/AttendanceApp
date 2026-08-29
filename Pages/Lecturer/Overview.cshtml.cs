using AttendanceApp.Data;
using AttendanceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Pages.Lecturer;

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

    public record SessionBar(string Label, DateOnly Date, int Present, int Status);

    public List<SessionBar> Bars { get; set; } = new();

    public async Task OnGetAsync()
    {
        var lecturerId = _userManager.GetUserId(User);

        var sessions = await _db.MeetingSessions
            .Include(s => s.Meeting)
            .Include(s => s.AttendanceRecords)
            .Where(s => s.Meeting!.LecturerId == lecturerId && s.Date <= DateOnly.FromDateTime(DateTime.Today))
            .OrderBy(s => s.Date)
            .ToListAsync();

        Bars = sessions.Select(s => new SessionBar(
            $"{s.Meeting!.Title} ({s.Date:d MMM})",
            s.Date,
            s.AttendanceRecords.Count(a => a.Status == AttendanceStatus.Present),
            0
        )).ToList();
    }
}
