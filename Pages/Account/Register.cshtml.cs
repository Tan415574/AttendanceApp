using System.ComponentModel.DataAnnotations;
using AttendanceApp.Data;
using AttendanceApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

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

    public IActionResult OnGet()
    {
        if (_signInManager.IsSignedIn(User))
        {
            var isLecturer = User.IsInRole("Lecturer");
            return isLecturer
                ? RedirectToPage("/Lecturer/Meetings/Index")
                : RedirectToPage("/Student/CheckIn");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.Role == "Student" && string.IsNullOrWhiteSpace(Input.StudentNumber))
            ModelState.AddModelError(nameof(Input.StudentNumber), "Student number is required for student accounts.");

        if (!ModelState.IsValid)
            return Page();

        // A lecturer's legacy-attendance import may have already created a placeholder
        // account for this student number (AttendanceImportService.IsPlaceholder) before
        // they ever signed up. Claim it — attach real credentials to that same row — rather
        // than creating a second, disconnected account that would orphan their imported
        // history.
        var placeholder = Input.Role == "Student"
            ? await _userManager.Users.FirstOrDefaultAsync(u => u.StudentNumber == Input.StudentNumber && u.IsPlaceholder)
            : null;

        if (placeholder is not null)
        {
            var passwordResult = await _userManager.AddPasswordAsync(placeholder, Input.Password);
            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }

            placeholder.FullName = Input.FullName;
            placeholder.IsPlaceholder = false;
            await _userManager.SetUserNameAsync(placeholder, Input.Email);
            await _userManager.SetEmailAsync(placeholder, Input.Email);
            await _userManager.UpdateAsync(placeholder);

            if (!await _userManager.IsInRoleAsync(placeholder, "Student"))
                await _userManager.AddToRoleAsync(placeholder, "Student");

            await _signInManager.SignInAsync(placeholder, isPersistent: true);
            return RedirectToPage("/Student/CheckIn");
        }

        // No placeholder to claim — but if this student number already belongs to a
        // real (already-claimed) account, registering again would silently create a
        // second, disconnected account under the same number. That would break the
        // "matching student number = same person's data everywhere" guarantee the
        // placeholder-claim flow exists for, so reject it with a clear message instead.
        if (Input.Role == "Student")
        {
            var alreadyClaimed = await _userManager.Users
                .AnyAsync(u => u.StudentNumber == Input.StudentNumber && !u.IsPlaceholder);
            if (alreadyClaimed)
            {
                ModelState.AddModelError(nameof(Input.StudentNumber),
                    "An account already exists for this student number. Sign in instead, or contact your lecturer if this is a mistake.");
                return Page();
            }
        }

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
