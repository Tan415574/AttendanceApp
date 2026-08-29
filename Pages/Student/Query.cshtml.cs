using System.ComponentModel.DataAnnotations;
using AttendanceApp.Data;
using AttendanceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Pages.Student;

[Authorize(Roles = "Student")]
public class QueryModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public QueryModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public MeetingSession? Session { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required] public int SessionId { get; set; }
        [Required, MinLength(5)] public string Message { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(int sessionId)
    {
        Session = await _db.MeetingSessions.Include(s => s.Meeting).FirstOrDefaultAsync(s => s.Id == sessionId);
        if (Session is null) return NotFound();
        Input.SessionId = sessionId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Session = await _db.MeetingSessions.Include(s => s.Meeting).FirstOrDefaultAsync(s => s.Id == Input.SessionId);
            return Page();
        }

        var studentId = _userManager.GetUserId(User)!;

        var existing = await _db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.MeetingSessionId == Input.SessionId && a.StudentId == studentId);

        if (existing is not null)
        {
            // Rare edge case: a record appeared after the page loaded (e.g. late self check-in).
            return RedirectToPage("/Student/Attendance");
        }

        _db.AttendanceRecords.Add(new AttendanceRecord
        {
            MeetingSessionId = Input.SessionId,
            StudentId = studentId,
            CheckedInAt = DateTime.UtcNow,
            Method = CheckInMethod.ManualCode,
            Status = AttendanceStatus.Disputed,
            QueryOpen = true,
            QueryMessage = Input.Message
        });
        await _db.SaveChangesAsync();

        return RedirectToPage("/Student/Attendance");
    }
}
