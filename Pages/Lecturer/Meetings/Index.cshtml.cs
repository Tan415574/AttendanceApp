using AttendanceApp.Data;
using AttendanceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Pages.Lecturer.Meetings;

[Authorize(Roles = "Lecturer")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<Meeting> Meetings { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        Meetings = await _db.Meetings
            .Where(m => m.LecturerId == userId)
            .OrderByDescending(m => m.StartDate)
            .ToListAsync();
    }
}
