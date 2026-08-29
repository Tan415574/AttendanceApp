<!-- ANSTAN - Tanit Ansara -->

# Design Decisions — INF3003W Attendance App

This document records the architecture and technical choices made while building the
attendance system, and the reasoning behind each one. It exists to satisfy the case
study's requirement that design decisions are made and owned by the developer, with AI
used as a collaborative tool rather than a decision-maker.

## 0. Where this diverges from the original AI-drafted plan

`ORIGINAL_SPEC.md` is the initial build prompt/plan (produced via a claude.ai web chat —
see Session 0 in `PROMPTS_APPENDIX.md`) that the scaffold was originally built from. The
most significant point where the implemented app **overrides** that plan:

- **Authentication.** The original spec explicitly says *"No user accounts or
  passwords — identity is just a student number, and everything else is trust-based."*
  That's not what got built: the app uses full ASP.NET Identity with real accounts and
  passwords (§1, §3 below). This was a deliberate developer decision, not an AI choice —
  the assignment brief requires "Individual Account Authentication," and a
  trust-based/passwordless model (anyone can check in as anyone by typing their student
  number) doesn't satisfy that requirement, regardless of how much simpler it would be
  to use. The one idea from the original spec that *was* kept: an account only needs to
  be created once (sign-up), after which returning to check in is just a normal sign-in.

## 1. Platform and stack

| Choice | Reasoning |
|---|---|
| ASP.NET Core 8, Razor Pages | Mandated by the case study brief. Razor Pages over MVC because the app is a set of fairly independent, page-scoped workflows (check-in, board, queries, overview) rather than a resource-oriented API — Razor Pages' one-page-one-model structure fits that better than MVC's controller/view split. |
| EF Core + SQLite | One week to deliver. SQLite needs no server setup, runs from a single file, and EF Core migrations give a repeatable schema without hand-written SQL. Trade-off accepted: SQLite doesn't handle high concurrent write load well — acceptable for a single-course classroom demo, not for a production multi-course deployment (see §6). |
| ASP.NET Identity | Required "Individual Account Authentication" with roles. Identity gives password hashing, role management, and cookie auth out of the box instead of hand-rolling auth — not worth reinventing for a one-week build. |
| SignalR | Chosen for the live board and check-in feed because polling for attendance updates would add latency and load for something that needs to feel instant to whoever is watching the board at the front of a lecture. |

## 2. Data model: Meeting vs MeetingSession

A `Meeting` is the lecturer's *template* — "Tuesday Workshop, weekly, 14:00" — not a
specific occurrence. A `MeetingSession` is one concrete dated instance, generated from
the template via `Meeting.GetOccurrenceDates()`.

**Why split them:** without this split, "weekly" recurrence would mean either
re-creating the same meeting every week by hand, or overloading a single row to somehow
represent every occurrence at once. Splitting template from occurrence means "start
today's session" is just opening the `MeetingSession` row that already exists for
today, and a lecturer can look at attendance for one specific Tuesday without it being
entangled with every other Tuesday.

**Accepted limitation:** `Meeting.DayOfWeek` is derived from the template's
`StartDate.DayOfWeek`. If a lecturer picks a start date that happens to be a Monday for
a meeting they intend to recur weekly, every occurrence lands on Monday — this is
correct behaviour, but not obvious from the UI, and should probably get a visible label
("repeats every Monday") so it doesn't surprise lecturers.

## 3. Attendance integrity

Two decisions here work together to make attendance hard to fake or duplicate:

1. **DB-level uniqueness, not just application logic.** `AttendanceRecord` has a unique
   index on `(MeetingSessionId, StudentId)` (`ApplicationDbContext.cs`). Even if two
   check-in requests race, the database itself rejects the duplicate rather than relying
   on an application check that could be beaten by a timing race.
2. **Cross-checking identity against the logged-in account.** On check-in, the typed
   student number is compared against the signed-in account's stored student number
   (`CheckIn.cshtml.cs`) — not just accepted at face value. This stops one student
   entering someone else's number from their own phone/session.

**Join codes** exclude visually ambiguous characters (`0/O`, `1/I/L` —
`JoinCodeGenerator.cs`) because the code is read off a projector screen and typed in
manually as a fallback to QR scanning; ambiguous characters would cause avoidable
failed check-ins.

## 4. Live board (the innovation feature)

The board page shows students appearing in real time as animated avatars
(matter.js physics) as they check in, driven by SignalR (`AttendanceHub.cs`,
`board.js`). This goes beyond the base requirement of "record attendance" to give the
lecturer an at-a-glance, engaging view of who's arrived during a live session — closer
to how a lecturer would visually scan a room than a static list would be.

**Why grouped by session, not broadcast to everyone:** the hub groups connections by
`session-{id}` so a lecturer's board only receives events for the specific session open
on their screen, not every check-in happening system-wide across other lecturers'
concurrent sessions.

**Visual design — bubbles dropping in, not a list.** Each student is rendered as a
small coloured shape (mostly soft circles, plus a few triangles/squares/hexagons for
variety — `board.js`) that drops from the top of the canvas under gravity and settles
among the others already there, labelled with their initials. This was a deliberate UX
choice over a plain growing list of names:

- A live, physical pile of avatars reads at a glance as "the room filling up" — closer
  to how a lecturer visually scans a room than scrolling a list would be.
- Initials only (not full names) keep each shape small enough to tile many students on
  one screen, and avoid the board becoming a wall of text during a large lecture.
- Distinct colours/shapes per `AvatarAssigner` index make it easy to visually spot
  "did someone new just join" without reading every label.
- Physics settling (matter.js) means the board needs no manual layout logic — bodies
  naturally stack and avoid overlapping as more students check in.

Trade-off accepted: initials aren't unique, so two students with the same initials look
identical on the board. Acceptable since the board is a live-glance tool, not the
attendance record of truth — the actual record is the `AttendanceRecord` row, which is
unambiguous.

## 5. Query / dispute workflow

A student who was marked absent (or not marked at all) can raise a query against a
specific session (`Pages/Student/Query.cshtml.cs`), which creates an
`AttendanceRecord` in a `Disputed` status with `QueryOpen = true`. The lecturer then
accepts (flips to Present) or rejects it (`Pages/Lecturer/Queries.cshtml.cs`).

**Why a record instead of a separate "dispute" table:** attendance and disputed
attendance are the same underlying fact — "was this student present" — just with an
extra state. Reusing `AttendanceRecord` with a status field avoids maintaining two
parallel representations of the same thing and keeps "does this student have a record
for this session" a single query.

## 6. Historical data import

`AttendanceImportService.cs` parses a wide-format legacy spreadsheet (student
name/number as leading columns, one column per date, `1`/`0` per cell) and unpivots it
into `MeetingSession` + `AttendanceRecord` rows.

**Format decision — CSV, not native `.xlsx`.** The importer reads CSV (auto-detecting
`,` vs `;` since exports vary by locale/Excel region settings) rather than parsing the
Excel binary format directly. CSV is what Excel exports to, and avoids pulling in a
binary-format parsing library for a one-week build. **This is a known gap against the
brief's literal wording ("upload an excel spreadsheet") and needs a decision**: either
CSV-from-Excel is an acceptable interpretation, or `.xlsx` parsing needs to be added
before submission.

**Conflict policy on re-import:** re-importing the same data is an upsert — a `1` cell
creates/overwrites a record as Present, a `0` cell deletes any existing record (absence
is represented by the *absence* of a record everywhere else in the app, so this stays
consistent). The one exception: a record with an open student query is left untouched
and reported back to the lecturer, so a bulk historical import can never silently
overwrite an active dispute the lecturer hasn't looked at yet.

## 7. Deliberately out of scope

These were left unbuilt as conscious scope decisions, not oversights:

- **Course/module enrolment scoping** — every student can currently see/check into
  every meeting. Fine for a single-course demo matching this assignment's context; not
  fine for a real multi-course deployment. Flagged rather than silently assumed.
- **Deployment/hosting configuration** — out of scope for a one-week academic
  deliverable; the app is built to run locally via `dotnet run`.
- **Email confirmation / password reset** — `RequireConfirmedAccount = false` in
  `Program.cs`. Acceptable for a one-week assignment demo; would need revisiting for a
  production rollout since account recovery has no path currently.

## 8. AI's role in this build

AI (Claude, via Claude Code) was used to accelerate implementation of decisions made
above — generating boilerplate, wiring EF Core relationships, and implementing the
SignalR/matter.js integration — but the architecture choices in this document (Razor
Pages over MVC, the Meeting/MeetingSession split, DB-level uniqueness for attendance
integrity, reusing AttendanceRecord for disputes instead of a separate table, and the
scope cuts in §7) were directed by the developer, not generated unprompted by the
AI. See the prompts appendix for the specific prompts used.
