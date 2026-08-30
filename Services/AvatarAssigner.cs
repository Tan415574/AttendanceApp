namespace AttendanceApp.Services;

// Maps each student to one of 12 blob "characters" (shape + colour), styled after
// the mood-icon reference: soft rounded shapes, flat colour, simple eyes.
// Deterministic so the same student always renders as the same character.
public static class AvatarAssigner
{
    public static readonly AvatarDef[] Avatars =
    {
        new("blob-round",   "#F17FB0", "excited"),
        new("blob-clover",  "#9B8CFB", "joyful"),
        new("blob-wave",    "#5AC8FA", "grateful"),
        new("blob-cloud",   "#3FD9C7", "energized"),
        new("blob-round",   "#6C7BF0", "sensitive"),
        new("blob-hex",     "#F5B942", "confused"),
        new("blob-round",   "#8EF07F", "bored"),
        new("blob-triangle","#5FCE63", "stressed"),
        new("blob-square",  "#FF7A6B", "angry"),
        new("blob-round",   "#9B8CFB", "insecure"),
        new("blob-pill",    "#F17FB0", "hurt"),
        new("blob-round",   "#5AC8FA", "guilty"),
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
