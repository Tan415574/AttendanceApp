using AttendanceApp.Data;
using AttendanceApp.Hubs;
using AttendanceApp.Models;
using AttendanceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Pages.Lecturer;

[Authorize(Roles = "Lecturer")]
public class BoardModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly QrCodeService _qr;
    private readonly IHubContext<AttendanceHub> _hub;

    public BoardModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, QrCodeService qr, IHubContext<AttendanceHub> hub)
    {
        _db = db;
        _userManager = userManager;
        _qr = qr;
        _hub = hub;
    }

    public Meeting Meeting { get; set; } = default!;
    public MeetingSession? TodaySession { get; set; }
    public string? QrDataUrl { get; set; }
    public List<AttendanceRecord> CheckedIn { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int meetingId)
    {
        var userId = _userManager.GetUserId(User);
        var meeting = await _db.Meetings
            .Include(m => m.Sessions)
            .FirstOrDefaultAsync(m => m.Id == meetingId && m.LecturerId == userId);

        if (meeting is null) return NotFound();
        Meeting = meeting;

        var today = DateOnly.FromDateTime(DateTime.Today);
        // Starting a meeting always means "today", regardless of what dates this meeting's
        // recurrence originally planned for — so today's session always exists once you land
        // on this page, created on the spot if it doesn't already.
        TodaySession = meeting.Sessions.FirstOrDefault(s => s.Date == today);
        if (TodaySession is null)
        {
            TodaySession = new MeetingSession { MeetingId = meeting.Id, Date = today, IsOpen = false };
            _db.MeetingSessions.Add(TodaySession);
            await _db.SaveChangesAsync();
        }

        if (TodaySession.IsOpen)
        {
            QrDataUrl = _qr.GeneratePngDataUrl(_qr.BuildCheckInUrl(Request, TodaySession.JoinCode));
            CheckedIn = await _db.AttendanceRecords
                .Include(a => a.Student)
                .Where(a => a.MeetingSessionId == TodaySession.Id)
                .OrderBy(a => a.CheckedInAt)
                .ToListAsync();
        }

        return Page();
    }

    // Opens today's session for this meeting — creating it first if this is the first time
    // it's been started — and generates a fresh join code so the check-in endpoint starts
    // accepting scans/codes for it. Always lands the lecturer on the QR screen.
    public async Task<IActionResult> OnPostStartAsync(int meetingId)
    {
        var meeting = await _db.Meetings.FirstOrDefaultAsync(m => m.Id == meetingId);
        if (meeting is null || meeting.LecturerId != _userManager.GetUserId(User))
            return Forbid();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var session = await _db.MeetingSessions.FirstOrDefaultAsync(s => s.MeetingId == meetingId && s.Date == today);
        if (session is null)
        {
            session = new MeetingSession { MeetingId = meetingId, Date = today };
            _db.MeetingSessions.Add(session);
        }

        session.JoinCode = JoinCodeGenerator.Generate();
        session.IsOpen = true;
        session.OpenedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return RedirectToPage(new { meetingId });
    }

    public async Task<IActionResult> OnPostCloseAsync(int sessionId)
    {
        var session = await _db.MeetingSessions.Include(s => s.Meeting)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null || session.Meeting!.LecturerId != _userManager.GetUserId(User))
            return Forbid();

        session.IsOpen = false;
        session.ClosedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return RedirectToPage(new { meetingId = session.MeetingId });
    }
}
