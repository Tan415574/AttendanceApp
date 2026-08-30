using System.Globalization;
using System.Text;
using AttendanceApp.Data;
using AttendanceApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Services;

// Parses a legacy attendance spreadsheet (wide format: Student Name;Student No;<date>;<date>...;
// 1/0 per cell) and unpivots it into MeetingSession + AttendanceRecord rows under one target
// Meeting. Auto-detects ',' vs ';' as the delimiter since exports vary by locale.
//
// Conflict policy (lecturer's call, not inferred): re-importing UPSERTS — a "1" cell creates or
// overwrites the record as Present, a "0" cell deletes any existing record for that student/date
// (absence is represented by the lack of a record, matching how the rest of the app treats it).
// The one exception: a record with an open student query is left untouched and reported, so a
// bulk historical import can never silently steamroll an active dispute.
//
// Unrecognized student numbers create a placeholder ApplicationUser (see IsPlaceholder) rather
// than being skipped — a legacy import happens precisely because those students haven't signed
// up on this app yet, so refusing to import their history until they do would defeat the point.
public class AttendanceImportService
{
    private static readonly string[] DateFormats =
    {
        "yyyy/MM/dd", "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "d/M/yyyy", "yyyy/M/d"
    };

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AttendanceImportService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<ImportResult> ImportAsync(int meetingId, Stream csvStream)
    {
        var messages = new List<string>();
        var meeting = await _db.Meetings.FirstOrDefaultAsync(m => m.Id == meetingId);
        if (meeting is null)
        {
            messages.Add("Meeting not found.");
            return new ImportResult(0, 0, 0, 0, 0, 0, messages);
        }

        string text;
        using (var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            text = await reader.ReadToEndAsync();

        var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).ToList();
        if (lines.Count < 2)
        {
            messages.Add("File has no data rows.");
            return new ImportResult(0, 0, 0, 0, 0, 0, messages);
        }

        char delimiter = lines[0].Count(c => c == ';') > lines[0].Count(c => c == ',') ? ';' : ',';
        var header = SplitLine(lines[0], delimiter);

        // Columns 0/1 are name/student number by convention; everything from column 2 on is a date.
        var dateColumns = new List<(int ColumnIndex, DateOnly Date)>();
        for (int col = 2; col < header.Count; col++)
        {
            if (TryParseDate(header[col].Trim(), out var date))
                dateColumns.Add((col, date));
            else
                messages.Add($"Column {col + 1} header \"{header[col]}\" isn't a recognizable date — that column was skipped.");
        }

        if (dateColumns.Count == 0)
        {
            messages.Add("No valid date columns found in the header row.");
            return new ImportResult(0, 0, 0, 0, 0, 0, messages);
        }

        // Get-or-create a MeetingSession per date column, then save so new ones get real Ids
        // before we look up/attach AttendanceRecords against them.
        var dates = dateColumns.Select(d => d.Date).ToList();
        var sessionsByDate = await _db.MeetingSessions
            .Where(s => s.MeetingId == meetingId && dates.Contains(s.Date))
            .ToDictionaryAsync(s => s.Date);

        int sessionsCreated = 0;
        foreach (var date in dates)
        {
            if (sessionsByDate.ContainsKey(date)) continue;
            var session = new MeetingSession
            {
                MeetingId = meetingId,
                Date = date,
                IsOpen = false,
                JoinCode = JoinCodeGenerator.Generate()
            };
            _db.MeetingSessions.Add(session);
            sessionsByDate[date] = session;
            sessionsCreated++;
        }
        if (sessionsCreated > 0)
            await _db.SaveChangesAsync();

        var sessionIds = sessionsByDate.Values.Select(s => s.Id).ToList();

        // Preload students by number, and existing records for the sessions in play, so the
        // per-cell loop below is all in-memory — no query-per-cell against ~2,700 cells.
        var studentIdByNumber = await _db.Users
            .Where(u => u.StudentNumber != null)
            .ToDictionaryAsync(u => u.StudentNumber!.Trim(), u => u.Id, StringComparer.OrdinalIgnoreCase);

        var existingRecords = await _db.AttendanceRecords
            .Where(a => sessionIds.Contains(a.MeetingSessionId))
            .ToDictionaryAsync(a => (a.MeetingSessionId, a.StudentId));

        int present = 0, absentRemoved = 0, placeholdersCreated = 0, invalidCells = 0, conflictsSkipped = 0;
        var placeholderFailureMessages = new List<string>();

        for (int row = 1; row < lines.Count; row++)
        {
            var fields = SplitLine(lines[row], delimiter);
            if (fields.Count < 2) continue;

            var studentNo = fields[1].Trim();
            var studentName = fields[0].Trim();
            if (studentNo.Length == 0) continue;

            if (!studentIdByNumber.TryGetValue(studentNo, out var studentId))
            {
                // Not registered yet — create a placeholder account so this student's history
                // isn't lost. A real signup with this student number later "claims" it
                // (Pages/Account/Register.cshtml.cs) instead of starting a blank new account.
                var placeholder = new ApplicationUser
                {
                    UserName = studentNo,
                    FullName = studentName.Length > 0 ? studentName : studentNo,
                    StudentNumber = studentNo,
                    AvatarIndex = AvatarAssigner.AssignIndex(studentNo),
                    IsPlaceholder = true
                };
                var createResult = await _userManager.CreateAsync(placeholder);
                if (!createResult.Succeeded)
                {
                    if (placeholderFailureMessages.Count < 20)
                        placeholderFailureMessages.Add($"Row {row + 1}: couldn't create an account for student number \"{studentNo}\" ({studentName}) — {string.Join(", ", createResult.Errors.Select(e => e.Description))}. Row skipped.");
                    continue;
                }
                await _userManager.AddToRoleAsync(placeholder, "Student");
                studentIdByNumber[studentNo] = placeholder.Id;
                studentId = placeholder.Id;
                placeholdersCreated++;
            }

            foreach (var (col, date) in dateColumns)
            {
                if (col >= fields.Count) continue;
                var raw = fields[col].Trim();
                if (raw.Length == 0) continue;

                var session = sessionsByDate[date];
                var key = (session.Id, studentId);
                existingRecords.TryGetValue(key, out var existing);

                if (existing is not null && existing.QueryOpen)
                {
                    conflictsSkipped++;
                    continue; // never let a bulk import steamroll an active student dispute
                }

                if (raw == "1")
                {
                    if (existing is null)
                    {
                        var record = new AttendanceRecord
                        {
                            MeetingSessionId = session.Id,
                            StudentId = studentId,
                            CheckedInAt = date.ToDateTime(meeting.TimeOfDay),
                            Method = CheckInMethod.Import,
                            Status = AttendanceStatus.Present
                        };
                        _db.AttendanceRecords.Add(record);
                        existingRecords[key] = record;
                    }
                    else
                    {
                        existing.Status = AttendanceStatus.Present;
                        existing.Method = CheckInMethod.Import;
                    }
                    present++;
                }
                else if (raw == "0")
                {
                    if (existing is not null)
                    {
                        _db.AttendanceRecords.Remove(existing);
                        existingRecords.Remove(key);
                        absentRemoved++;
                    }
                }
                else
                {
                    invalidCells++;
                }
            }
        }

        await _db.SaveChangesAsync();

        messages.AddRange(placeholderFailureMessages);

        return new ImportResult(sessionsCreated, present, absentRemoved, placeholdersCreated, invalidCells, conflictsSkipped, messages);
    }

    private static bool TryParseDate(string text, out DateOnly date)
    {
        if (DateOnly.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;
        return DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    // Minimal quoted-field CSV/delimited-line splitter — handles plain files like the sample
    // export without pulling in a NuGet dependency for something this small.
    private static List<string> SplitLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == delimiter) { fields.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
        }
        fields.Add(sb.ToString());
        return fields;
    }
}

public record ImportResult(
    int SessionsCreated,
    int RecordsMarkedPresent,
    int RecordsMarkedAbsent,
    int PlaceholderAccountsCreated,
    int InvalidCells,
    int ConflictsSkipped,
    List<string> Messages);
