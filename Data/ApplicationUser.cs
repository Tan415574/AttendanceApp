using Microsoft.AspNetCore.Identity;

namespace AttendanceApp.Data;

// Extends IdentityUser rather than building a parallel Student/Lecturer table,
// so ASP.NET Identity handles password hashing, login, etc. out of the box.
// Role is enforced via Identity roles ("Student" / "Lecturer"); StudentNumber
// is only populated for students and is what shows up on the check-in board.
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    // Null for lecturers.
    public string? StudentNumber { get; set; }

    // Deterministic index into the avatar set (0-11), assigned once at registration
    // so a student always "looks the same" on the board. See Services/AvatarAssigner.cs.
    public int AvatarIndex { get; set; }

    // True for a student who exists only because a lecturer's CSV import referenced their
    // student number before they ever signed up themselves (Services/AttendanceImportService.cs).
    // Has no password set yet. When a real student registers with the same student number,
    // Pages/Account/Register.cshtml.cs "claims" this row (attaches real credentials to it)
    // instead of creating a second, disconnected account - so imported history isn't orphaned.
    public bool IsPlaceholder { get; set; }
}
