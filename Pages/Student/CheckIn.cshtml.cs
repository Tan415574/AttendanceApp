using System.ComponentModel.DataAnnotations;
using AttendanceApp.Data;
using AttendanceApp.Hubs;
using AttendanceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Pages.Student;

[Authorize(Roles = "Student")]
public class CheckInModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHubContext<AttendanceHub> _hub;

    public CheckInModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IHubContext<AttendanceHub> hub)
    {
        _db = db;
        _userManager = userManager;
        _hub = hub;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? SuccessMessage { get; set; }

    public class InputModel
    {
        [Required] public string Code { get; set; } = string.Empty;
        [Required] public string StudentNumber { get; set; } = string.Empty;
        public bool ScannedByCamera { get; set; }
    }

    public void OnGet(string? code)
    {
        if (!string.IsNullOrWhiteSpace(code))
            Input.Code = code.ToUpperInvariant();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null) return Challenge();

        // Cross-check the typed student number against the signed-in account rather than
        // trusting it blindly — stops one student checking someone else in from their phone.
        if (!string.Equals(currentUser.StudentNumber, Input.StudentNumber, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(Input.StudentNumber), "That student number doesn't match your signed-in account.");
            return Page();
        }

        var session = await _db.MeetingSessions
            .Include(s => s.Meeting)
            .FirstOrDefaultAsync(s => s.JoinCode == Input.Code.ToUpperInvariant() && s.IsOpen);

        if (session is null)
        {
            ModelState.AddModelError(nameof(Input.Code), "That code isn't active. Check it's typed correctly and the lecturer has started the meeting.");
            return Page();
        }

        var alreadyIn = await _db.AttendanceRecords
            .AnyAsync(a => a.MeetingSessionId == session.Id && a.StudentId == currentUser.Id);

        if (alreadyIn)
        {
            SuccessMessage = "You're already checked in for this session.";
            return Page();
        }

        var record = new AttendanceRecord
        {
            MeetingSessionId = session.Id,
            StudentId = currentUser.Id,
            CheckedInAt = DateTime.UtcNow,
            Method = Input.ScannedByCamera ? CheckInMethod.QrScan : CheckInMethod.ManualCode
        };
        _db.AttendanceRecords.Add(record);
        await _db.SaveChangesAsync();

        var totalCheckedIn = await _db.AttendanceRecords.CountAsync(a => a.MeetingSessionId == session.Id);

        await _hub.Clients.Group(AttendanceHub.GroupName(session.Id))
            .SendAsync("CheckIn", new
            {
                studentName = currentUser.FullName,
                studentNumber = currentUser.StudentNumber,
                avatarIndex = currentUser.AvatarIndex,
                totalCheckedIn
            });

        SuccessMessage = $"You're marked present for {session.Meeting!.Title}.";
        return Page();
    }
}
