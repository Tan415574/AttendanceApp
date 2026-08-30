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
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AttendanceImportService _importer;

    public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, AttendanceImportService importer)
    {
        _db = db;
        _userManager = userManager;
        _importer = importer;
    }

    public List<Meeting> Meetings { get; set; } = new();

    [BindProperty]
    public IFormFile? ImportFile { get; set; }

    public ImportResult? ImportResultSummary { get; set; }
    public string? ImportError { get; set; }

    public async Task OnGetAsync()
    {
        await LoadMeetings();
    }

    // The single "bring in my old spreadsheet" button on the landing page. No meeting
    // needs to exist first — this creates one on the fly (titled from the file name)
    // to hold whatever sessions the sheet's date columns need, then unpivots every
    // student's attendance out of it in one shot.
    public async Task<IActionResult> OnPostImportAsync()
    {
        if (ImportFile is null || ImportFile.Length == 0)
        {
            ImportError = "Choose a CSV file first.";
            await LoadMeetings();
            return Page();
        }

        var lecturerId = _userManager.GetUserId(User)!;
        var title = Path.GetFileNameWithoutExtension(ImportFile.FileName);
        if (string.IsNullOrWhiteSpace(title)) title = "Imported attendance";

        var meeting = new Meeting
        {
            Title = title,
            Description = "Created automatically from a historical attendance import.",
            Type = MeetingType.Lecture,
            Recurrence = RecurrencePattern.OnceOff,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            TimeOfDay = new TimeOnly(0, 0),
            LecturerId = lecturerId
        };
        _db.Meetings.Add(meeting);
        await _db.SaveChangesAsync();

        await using var stream = ImportFile.OpenReadStream();
        ImportResultSummary = await _importer.ImportAsync(meeting.Id, stream);

        await LoadMeetings();
        return Page();
    }

    private async Task LoadMeetings()
    {
        var userId = _userManager.GetUserId(User);
        Meetings = await _db.Meetings
            .Where(m => m.LecturerId == userId)
            .OrderByDescending(m => m.StartDate)
            .ToListAsync();
    }
}
