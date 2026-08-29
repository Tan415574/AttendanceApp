namespace AttendanceApp.Models;

public enum MeetingType
{
    Lecture,
    Workshop,
    Test
}

public enum RecurrencePattern
{
    OnceOff,
    Weekly
}

public enum CheckInMethod
{
    QrScan,
    ManualCode,
    Import   // backfilled from a lecturer's legacy spreadsheet, not a live check-in
}

public enum AttendanceStatus
{
    Present,
    Disputed   // student has raised a query about a missed session; not counted as present until a lecturer resolves it
}
