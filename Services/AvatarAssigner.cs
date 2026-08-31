namespace AttendanceApp.Services;

// Maps each student to one of 12 mascot blobs, styled after the Roll Call mood-icon
// reference: one blob shape, colored from the 5-color palette, face drawn client-side
// (wwwroot/js/board.js — keep the two lists in sync if you add avatars).
// Deterministic so the same student always renders as the same character.
public static class AvatarAssigner
{
    public static readonly AvatarDef[] Avatars =
    {
        new("blob", "#FF6B4A", "coral"),
        new("blob", "#6BCB77", "grass"),
        new("blob", "#FFC93C", "sunny"),
        new("blob", "#4DA6FF", "sky"),
        new("blob", "#FF6FA5", "bubblegum"),
        new("blob", "#6BCB77", "grass"),
        new("blob", "#FF6B4A", "coral"),
        new("blob", "#4DA6FF", "sky"),
        new("blob", "#FFC93C", "sunny"),
        new("blob", "#FF6FA5", "bubblegum"),
        new("blob", "#6BCB77", "grass"),
        new("blob", "#FF6B4A", "coral"),
    };

    public static int AssignIndex(string studentNumber)
    {
        unchecked
        {
            int hash = 17;
            foreach (var c in studentNumber)
                hash = hash * 31 + c;
            return Math.Abs(hash) % Avatars.Length;
        }
    }

    public record AvatarDef(string Shape, string Color, string Label);
}
