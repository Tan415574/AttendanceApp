using System.ComponentModel.DataAnnotations;
using AttendanceApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AttendanceApp.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, isPersistent: true, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Incorrect email or password.");
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        var isLecturer = user is not null && await _userManager.IsInRoleAsync(user, "Lecturer");

        return isLecturer
            ? RedirectToPage("/Lecturer/Meetings/Index")
            : RedirectToPage("/Student/CheckIn");
    }
}
