namespace AttendanceApp.Services;

// Maps each student to one of 12 blob "characters" (shape + colour), styled after
// the mood-icon reference: soft rounded shapes, flat colour, simple eyes.
// Deterministic so the same student always renders as the same character.
public static class AvatarAssigner
{
    public static readonly AvatarDef[] Avatars =
    {
        new("blob-round",   "#F6A8D8", "excited"),
        new("blob-clover",  "#EC6FB6", "joyful"),
        new("blob-wave",    "#B58AD9", "grateful"),
        new("blob-cloud",   "#C9B6F0", "energized"),
        new("blob-round",   "#38B6E8", "sensitive"),
        new("blob-hex",     "#1FA9C9", "confused"),
        new("blob-round",   "#2FA84F", "bored"),
        new("blob-triangle","#3D9B4A", "stressed"),
        new("blob-square",  "#E8641C", "angry"),
        new("blob-round",   "#F08A1C", "insecure"),
        new("blob-pill",    "#F5A623", "hurt"),
        new("blob-round",   "#F6C93B", "guilty"),
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
