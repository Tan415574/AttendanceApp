using System.ComponentModel.DataAnnotations;
using AttendanceApp.Data;
using AttendanceApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AttendanceApp.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required] public string Role { get; set; } = "Student";
        [Required] public string FullName { get; set; } = string.Empty;
        public string? StudentNumber { get; set; }
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.Role == "Student" && string.IsNullOrWhiteSpace(Input.StudentNumber))
            ModelState.AddModelError(nameof(Input.StudentNumber), "Student number is required for student accounts.");

        if (!ModelState.IsValid)
            return Page();

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            FullName = Input.FullName,
            StudentNumber = Input.Role == "Student" ? Input.StudentNumber : null,
            AvatarIndex = Input.Role == "Student" ? AvatarAssigner.AssignIndex(Input.StudentNumber!) : 0
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        await _userManager.AddToRoleAsync(user, Input.Role);
        await _signInManager.SignInAsync(user, isPersistent: true);

        return Input.Role == "Lecturer"
            ? RedirectToPage("/Lecturer/Meetings/Index")
            : RedirectToPage("/Student/CheckIn");
    }
}
