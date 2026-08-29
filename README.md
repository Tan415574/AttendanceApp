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
- Historical attendance import: upload a **CSV** (wide format, one column per date)
  and it's unpivoted into `MeetingSession`/`AttendanceRecord` rows, with an
  upsert-on-reimport policy that never overwrites an open dispute
  (`Pages/Lecturer/Meetings/Import.*`, `Services/AttendanceImportService.cs`).
  Note: the brief asks for "an excel spreadsheet" and this reads CSV, not native
  `.xlsx` — confirm that's an acceptable interpretation before submitting, or add
  real `.xlsx` parsing.

**Not built — deliberately left, per the case study's "AI shouldn't make your
decisions" instruction:**
- Course/module enrolment scoping (right now every student can see/check into every
  meeting; fine for a single-course demo, not for a real multi-course deployment)
- Any deployment/hosting config

## Known rough edges to check before you rely on this

- `Meeting.DayOfWeek` is derived from `StartDate.DayOfWeek` — if a lecturer picks a
  Monday-dated start for a "weekly" meeting, all occurrences land on Mondays. That's
  intentional but worth surfacing in the UI (e.g. a label showing "repeats every
  Monday") so it's not surprising.
- The board page assumes one session per meeting per calendar day. If you need two
  sessions of the same meeting on the same day, `BoardModel.OnGetAsync`'s
  `FirstOrDefault(s => s.Date == today)` needs to become a session picker instead.
- No email confirmation / password reset flow — fine for a one-week assignment,
  flag it explicitly in your reflection if the rubric cares about production-readiness.

## Verification

Ran a full end-to-end walk-through in a real headless browser (Playwright): register
lecturer → create meeting → start session → register student → check in via join code
→ confirm the live board updates in real time (no reload) → confirm the student's
calendar shows the attendance. All steps passed.

**One real bug found and fixed in the process:** `Pages/Index.cshtml` (the `/` route)
was missing the `@model AttendanceApp.Pages.IndexModel` directive that every other page
in this app has. Without it, Razor Pages never bound `IndexModel` to the view, so
`OnGet()` — which redirects signed-in users to their home page and everyone else to
`/Account/Login` — was never invoked. The homepage silently rendered its (empty)
template and returned `200` with no content instead of redirecting. This meant the
root URL never worked, for anyone, the whole time — `dotnet build` doesn't catch it
since it's a Razor Page runtime-binding issue, not a compile error. Fixed by adding the
missing `@model` line.

## UML artefacts

- `attendance_capture_sequence.mermaid` — sequence diagram of the student check-in
  flow (join-code validation, duplicate check, DB write, SignalR broadcast to the
  live board).
- `attendance_class_diagram.mermaid` — class diagram of the
  `Meeting` → `MeetingSession` → `AttendanceRecord` data model and how it relates to
  `ApplicationUser`.

Render either with a Mermaid live editor (mermaid.live) or any Markdown viewer/IDE
extension that supports Mermaid fences.
