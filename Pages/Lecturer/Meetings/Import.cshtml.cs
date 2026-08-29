using AttendanceApp.Data;
using AttendanceApp.Models;
using AttendanceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Pages.Lecturer.Meetings;

[Authorize(Roles = "Lecturer")]
public class ImportModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AttendanceImportService _importer;

    public ImportModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, AttendanceImportService importer)
    {
        _db = db;
        _userManager = userManager;
        _importer = importer;
    }

    public Meeting Meeting { get; set; } = default!;

    [BindProperty]
    public IFormFile? UploadFile { get; set; }

    public ImportResult? Result { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int meetingId)
    {
        var meeting = await LoadOwnedMeeting(meetingId);
        if (meeting is null) return NotFound();
        Meeting = meeting;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int meetingId)
    {
        var meeting = await LoadOwnedMeeting(meetingId);
        if (meeting is null) return NotFound();
        Meeting = meeting;

        if (UploadFile is null || UploadFile.Length == 0)
        {
            ErrorMessage = "Choose a CSV file to upload first.";
            return Page();
        }

        await using var stream = UploadFile.OpenReadStream();
        Result = await _importer.ImportAsync(meetingId, stream);
        return Page();
    }

    private async Task<Meeting?> LoadOwnedMeeting(int meetingId)
    {
        var lecturerId = _userManager.GetUserId(User);
        return await _db.Meetings.FirstOrDefaultAsync(m => m.Id == meetingId && m.LecturerId == lecturerId);
    }
}
