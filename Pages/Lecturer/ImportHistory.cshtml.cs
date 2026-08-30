using AttendanceApp.Data;
using AttendanceApp.Models;
using AttendanceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Pages.Lecturer;

// Standalone "bring your old spreadsheet in" entry point — no need to have already
// created a Meeting first. One upload creates whatever sessions the sheet's date
// columns need and works out each student's attendance from it directly, per date,
// per student (AttendanceImportService.cs does the actual unpivoting).
[Authorize(Roles = "Lecturer")]
public class ImportHistoryModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AttendanceImportService _importer;

    public ImportHistoryModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, AttendanceImportService importer)
    {
        _db = db;
        _userManager = userManager;
        _importer = importer;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public ImportResult? Result { get; set; }
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required] public string CourseTitle { get; set; } = string.Empty;
        [Required] public MeetingType Type { get; set; } = MeetingType.Lecture;
        public IFormFile? UploadFile { get; set; }
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.UploadFile is null || Input.UploadFile.Length == 0)
        {
            ErrorMessage = "Choose a CSV file first.";
            return Page();
        }

        if (!ModelState.IsValid)
            return Page();

        var lecturerId = _userManager.GetUserId(User)!;

        // A lightweight container for the imported sessions to live under — same Meeting
        // entity every other feature already understands (board, overview graph, etc.),
        // just created on the fly instead of through the manual "New meeting" form. Its
        // own recurrence fields are placeholders; the actual session dates come from the
        // sheet, not from this template.
        var meeting = new Meeting
        {
            Title = Input.CourseTitle,
            Description = "Created automatically from a historical attendance import.",
            Type = Input.Type,
            Recurrence = RecurrencePattern.OnceOff,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            TimeOfDay = new TimeOnly(0, 0),
            LecturerId = lecturerId
        };
        _db.Meetings.Add(meeting);
        await _db.SaveChangesAsync();

        await using var stream = Input.UploadFile.OpenReadStream();
        Result = await _importer.ImportAsync(meeting.Id, stream);
        return Page();
    }
}
