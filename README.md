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
  and it's unpivoted into `MeetingSession`/`AttendanceRecord` rows per student per
  date, with an upsert-on-reimport policy that never overwrites an open dispute
  (`Services/AttendanceImportService.cs`). Two entry points: a standalone one-button
  flow for bringing in an entire historical spreadsheet before you've created any
  meetings at all (`Pages/Lecturer/ImportHistory.*` — creates its own course
  container automatically), and a per-meeting one for adding more history to a
  course you've already set up (`Pages/Lecturer/Meetings/Import.*`). A student
  number the import doesn't recognise gets a placeholder account (no password)
  rather than being skipped, so historical data for students who haven't signed up
  yet isn't lost — the first real registration with that student number claims the
  placeholder and inherits its history (`Pages/Account/Register.cshtml.cs`, see
  `ApplicationUser.IsPlaceholder`). Note: the brief asks for "an excel spreadsheet"
  and this reads CSV, not native `.xlsx` — confirm that's an acceptable
  interpretation before submitting, or add real `.xlsx` parsing.

**Not built — deliberately left, per the case study's "AI shouldn't make your
decisions" instruction:**
- Course/module enrolment scoping (right now every student can see/check into every
  meeting; fine for a single-course demo, not for a real multi-course deployment)
- Any deployment/hosting config

## Known rough edges to check before you rely on this

- `Meeting.DayOfWeek` is derived from `StartDate.DayOfWeek`, but it's now purely
  informational — starting a meeting always opens/creates *today's* session
  regardless of the planned recurrence (see Verification below), so this no longer
  blocks anyone, it just labels the originally-planned weekday.
- The board page assumes one session per meeting per calendar day — starting a
  meeting twice in one day reopens the same session rather than creating a second
  one. If you need two distinct sessions of the same meeting on the same day,
  `BoardModel`'s `s.Date == today` lookup needs to become a session picker instead.
- No email confirmation / password reset flow — fine for a one-week assignment,
  flag it explicitly in your reflection if the rubric cares about production-readiness.

## Verification

Ran full end-to-end walk-throughs in a real headless browser (Playwright) across
several passes: register lecturer → create meeting → start session → register student
→ check in via join code → confirm the live board updates in real time (no reload) →
confirm the student's calendar shows the attendance; and separately, standalone
historical import (no meeting pre-created) → 104-student CSV → claim a placeholder
account via normal registration → confirm their real historical calendar. All steps
pass as of the latest run.

**Real bugs found and fixed along the way:**
- `Pages/Index.cshtml` (the `/` route) was missing the
  `@model AttendanceApp.Pages.IndexModel` directive every other page has. Without it,
  Razor Pages never bound `IndexModel` to the view, so `OnGet()` — which redirects
  signed-in users to their home page and everyone else to `/Account/Login` — was never
  invoked. The homepage silently rendered its (empty) template and returned `200` with
  no content instead of redirecting, for anyone, the whole time. `dotnet build` can't
  catch this — it's a runtime binding gap, not a compile error.
- **Sign out never worked.** The shared layout's logout form
  (`Pages/_Layout.cshtml`) was plain HTML (`<form method="post" action="...">`)
  instead of using Razor's form tag helper (`asp-page="..."`), so it never got an
  antiforgery token. Razor Pages auto-validates antiforgery tokens on POST, so every
  click was rejected with `400` *before* `SignOutAsync()` ever ran — the button looked
  like it worked (redirected to a `/Account/Logout` page) but never actually signed
  anyone out. Fixed by switching to `asp-page="/Account/Logout"`.
- The CSV importer originally required a `Meeting` to already exist and skipped any
  student number it didn't recognise — see `DESIGN_DECISIONS.md` §6 for the full story
  (tested against a real 104-row sheet, 101 rows were silently dropped). Fixed with a
  standalone import entry point plus placeholder accounts.
- The board's "start session" flow could show a "Start meeting" button for a session
  days in the future with no date shown, or refuse to start anything at all if a
  meeting's planned recurrence didn't happen to land on today. Fixed: starting a
  meeting now always means today — the session is found-or-created for today on the
  spot, so clicking "Start meeting" always immediately shows the QR/join-code screen.

## UML artefacts

- `attendance_capture_sequence.mermaid` — sequence diagram of the student check-in
  flow (join-code validation, duplicate check, DB write, SignalR broadcast to the
  live board).
- `attendance_class_diagram.mermaid` — class diagram of the
  `Meeting` → `MeetingSession` → `AttendanceRecord` data model and how it relates to
  `ApplicationUser`.

Render either with a Mermaid live editor (mermaid.live) or any Markdown viewer/IDE
extension that supports Mermaid fences.
