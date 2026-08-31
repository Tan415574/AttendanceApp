namespace AttendanceApp.Models;

// Options for the shared _Mascot partial — a small decorative blob character used
// across several pages for "one shape, many faces" personality without any JS/canvas.
public class MascotOptions
{
    public string Shape { get; set; } = "circle"; // circle | square | blob
    public int Size { get; set; } = 48;
    public string Color { get; set; } = "var(--coral)";
    public string Face { get; set; } = "smile"; // smile | wide
    public string CssClass { get; set; } = "";
    public string Style { get; set; } = "";
}
