using AttendanceApp.Data;
using AttendanceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Pages.Lecturer;

[Authorize(Roles = "Lecturer")]
public class QueriesModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public QueriesModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<AttendanceRecord> OpenQueries { get; set; } = new();

    public async Task OnGetAsync()
    {
        var lecturerId = _userManager.GetUserId(User);
        OpenQueries = await _db.AttendanceRecords
            .Include(a => a.Student)
            .Include(a => a.MeetingSession).ThenInclude(s => s!.Meeting)
            .Where(a => a.QueryOpen && a.MeetingSession!.Meeting!.LecturerId == lecturerId)
            .OrderBy(a => a.CheckedInAt)
            .ToListAsync();
    }

    // "Accept" flips the record to Present — the student's dispute is upheld.
    public async Task<IActionResult> OnPostAcceptAsync(int recordId, string? response)
    {
        var record = await LoadOwnedRecord(recordId);
        if (record is null) return Forbid();

        record.Status = AttendanceStatus.Present;
        record.ManuallyAdjusted = true;
        record.QueryOpen = false;
        record.LecturerResponse = response;
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    // "Reject" closes the query but leaves the record as a (non-present) absence.
    public async Task<IActionResult> OnPostRejectAsync(int recordId, string? response)
    {
        var record = await LoadOwnedRecord(recordId);
        if (record is null) return Forbid();

        record.QueryOpen = false;
        record.LecturerResponse = response;
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task<AttendanceRecord?> LoadOwnedRecord(int recordId)
    {
        var lecturerId = _userManager.GetUserId(User);
        return await _db.AttendanceRecords
            .Include(a => a.MeetingSession).ThenInclude(s => s!.Meeting)
            .FirstOrDefaultAsync(a => a.Id == recordId && a.MeetingSession!.Meeting!.LecturerId == lecturerId);
    }
}
