<!-- ANSTAN - Tanit Ansara -->

# INF3003W Attendance App

ASP.NET Core 8 / Razor Pages / EF Core (SQLite) / SignalR.

## Setup

```bash
# from this folder
dotnet restore
dotnet tool install --global dotnet-ef   # if you don't already have it
dotnet ef migrations add Init
dotnet run
```

Then open the URL printed in the console (e.g. `https://localhost:5001`).

The app auto-creates the "Lecturer" and "Student" Identity roles on first run
(`Program.cs`). No seed users — sign up through the UI.

## What's here vs what's a stub

**Fully implemented:**
- Identity-based auth with Student/Lecturer roles (`Data/ApplicationUser.cs`,
  `Pages/Account/*`)
- Meeting definitions: type (Lecture/Workshop/Test), once-off or weekly recurrence,
  auto-expansion into dated `MeetingSession` rows (`Models/Meeting.cs`,
  `Pages/Lecturer/Meetings/Create.cshtml.cs`)
- Live board: join code + QR generation (QRCoder), physics-based avatar drop-in
  (matter.js) driven by real-time SignalR events (`Pages/Lecturer/Board.*`,
  `Hubs/AttendanceHub.cs`, `wwwroot/js/board.js`)
- Student check-in: camera scan (html5-qrcode) or manual code entry, cross-checked
  against the logged-in account, duplicate-proof at the DB level via a unique index
  (`Pages/Student/CheckIn.*`, `Data/ApplicationDbContext.cs`)
- Attendance calendar: green/red grid, click a missed day to raise a query
  (`Pages/Student/Attendance.*`, `Pages/Student/Query.*`)
- Lecturer query resolution: accept (flips to Present) / reject
  (`Pages/Lecturer/Queries.*`)
- Attendance-per-lecture bar graph (Chart.js) (`Pages/Lecturer/Overview.*`)

**Not built — deliberately left for you, per the case study's "AI shouldn't make
your decisions" instruction:**
- Spreadsheet upload/import of legacy attendance (the design doc describes the
  unpivot logic for this — see §5.3/§6.2 — but no code yet)
- Course/module enrolment scoping (right now every student can see/check into every
  meeting; fine for a single-course demo, not for a real multi-course deployment)
- Any deployment/hosting config

## Known rough edges to check before you rely on this

- **Not compiled.** I wrote this without .NET/NuGet access, so treat it as a strong
  draft — run `dotnet build` and expect to fix a handful of small errors (missing
  usings, minor API mismatches) rather than a guaranteed clean build.
- `Meeting.DayOfWeek` is derived from `StartDate.DayOfWeek` — if a lecturer picks a
  Monday-dated start for a "weekly" meeting, all occurrences land on Mondays. That's
  intentional but worth surfacing in the UI (e.g. a label showing "repeats every
  Monday") so it's not surprising.
- The board page assumes one session per meeting per calendar day. If you need two
  sessions of the same meeting on the same day, `BoardModel.OnGetAsync`'s
  `FirstOrDefault(s => s.Date == today)` needs to become a session picker instead.
- No email confirmation / password reset flow — fine for a one-week assignment,
  flag it explicitly in your reflection if the rubric cares about production-readiness.

## Suggested UML artefact for submission

`attendance_capture_sequence.mermaid` (from the earlier design doc deliverable)
covers the student self-capture decision flow. If you want a second artefact
showing the recurrence/meeting-session relationship, a class diagram of
`Meeting` → `MeetingSession` → `AttendanceRecord` (see `Models/`) would be quick
to produce from what's already built.
