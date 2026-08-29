using System.ComponentModel.DataAnnotations;
using AttendanceApp.Data;
using AttendanceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AttendanceApp.Pages.Lecturer.Meetings;

[Authorize(Roles = "Lecturer")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required] public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required] public MeetingType Type { get; set; }
        [Required] public RecurrencePattern Recurrence { get; set; }
        [Required] public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        [Required] public TimeOnly TimeOfDay { get; set; }
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.Recurrence == RecurrencePattern.Weekly && Input.EndDate is null)
            ModelState.AddModelError(nameof(Input.EndDate), "Weekly meetings need an end date to know how many occurrences to create.");

        if (Input.Recurrence == RecurrencePattern.Weekly && Input.EndDate < Input.StartDate)
            ModelState.AddModelError(nameof(Input.EndDate), "End date must be after the start date.");

        if (!ModelState.IsValid)
            return Page();

        var meeting = new Meeting
        {
            Title = Input.Title,
            Description = Input.Description,
            Type = Input.Type,
            Recurrence = Input.Recurrence,
            StartDate = Input.StartDate,
            EndDate = Input.Recurrence == RecurrencePattern.Weekly ? Input.EndDate : null,
            DayOfWeek = Input.Recurrence == RecurrencePattern.Weekly ? Input.StartDate.DayOfWeek : null,
            TimeOfDay = Input.TimeOfDay,
            LecturerId = _userManager.GetUserId(User)!
        };

        // Materialize every occurrence up front (rather than lazily on "start") so the
        // lecturer can see the full schedule immediately and students' calendars have
        // the full picture even before a session is opened.
        foreach (var date in meeting.GetOccurrenceDates())
        {
            meeting.Sessions.Add(new MeetingSession { Date = date, IsOpen = false });
        }

        _db.Meetings.Add(meeting);
        await _db.SaveChangesAsync();

        return RedirectToPage("/Lecturer/Meetings/Index");
    }
}
