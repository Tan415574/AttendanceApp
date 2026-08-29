using AttendanceApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AttendanceApp.Pages;

// Landing route ("/"). Sends signed-in users straight to their home page and
// everyone else to sign-in, so the layout's brand link has somewhere to go.
public class IndexModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public IndexModel(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public IActionResult OnGet()
    {
        if (!_signInManager.IsSignedIn(User))
            return RedirectToPage("/Account/Login");

        return User.IsInRole("Lecturer")
            ? RedirectToPage("/Lecturer/Meetings/Index")
            : RedirectToPage("/Student/CheckIn");
    }
}
