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

    // The auto-created meeting every single-button import lands in, identified by this
    // fixed Description so re-uploads (the same file re-checked, or a corrected version)
    // land back in the SAME meeting instead of spawning a duplicate one each time —
    // AttendanceImportService's own upsert logic (get-or-create session per date, replace
    // existing records) only works when it's handed the same meetingId every time.
    private const string ImportMeetingDescription = "Created automatically from a historical attendance import.";

    // The single "bring in my old spreadsheet" button on the landing page. No meeting
    // needs to exist first — this creates one on the fly (titled from the file name)
    // the first time, then reuses it on every later import, so unpivoting every
    // student's attendance out of a re-uploaded sheet updates the same dataset instead
    // of doubling it.
    public async Task<IActionResult> OnPostImportAsync()
    {
        if (ImportFile is null || ImportFile.Length == 0)
        {
            ImportError = "Choose a CSV file first.";
            await LoadMeetings();
            return Page();
        }

        var lecturerId = _userManager.GetUserId(User)!;

        var meeting = await _db.Meetings.FirstOrDefaultAsync(m =>
            m.LecturerId == lecturerId && m.Description == ImportMeetingDescription);

        if (meeting is null)
        {
            var title = Path.GetFileNameWithoutExtension(ImportFile.FileName);
            if (string.IsNullOrWhiteSpace(title)) title = "Imported attendance";

            meeting = new Meeting
            {
                Title = title,
                Description = ImportMeetingDescription,
                Type = MeetingType.Lecture,
                Recurrence = RecurrencePattern.OnceOff,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                TimeOfDay = new TimeOnly(0, 0),
                LecturerId = lecturerId
            };
            _db.Meetings.Add(meeting);
            await _db.SaveChangesAsync();
        }

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
