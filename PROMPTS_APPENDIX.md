<!-- ANSTAN - Tanit Ansara -->

# Appendix — AI Prompts Used

**Tool:** Claude Code
**Model:** Claude Sonnet 5 (`claude-sonnet-5`) — confirmed identical across all three
sessions below by checking each session's raw transcript.

This is the full, unedited list of prompts I gave Claude Code, in chronological order,
pulled directly from the local session transcripts
(`~/.claude/projects/c--Users-Admin-Downloads-AttendanceApp/*.jsonl`) rather than
reconstructed from memory. IDE context lines (e.g. "user opened file X") are included
where they appeared alongside a prompt, since they were part of what the AI saw.

> **Note on scope:** Session 0 below was reconstructed from screenshots of the original
> claude.ai web chat (provided by Tanit), not pulled automatically from a transcript file
> the way Sessions 1–3 were — claude.ai conversations aren't locally accessible the way
> Claude Code sessions are. If that web chat had more back-and-forth beyond what's
> captured here (e.g. follow-up refinements after the plan document was generated),
> those turns are not included — check the original conversation on claude.ai if a
> complete record is needed.
>
> The plan document that came out of prompt 0b is included in this repo as
> `ORIGINAL_SPEC.md`. Note it describes a **passwordless, student-number-only** identity
> model — the actual app deliberately overrides this with full ASP.NET Identity
> authentication; see `DESIGN_DECISIONS.md` §0 for why.
>
> **Resolved:** confirmed with Tanit that no separate design document with numbered
> sections ever existed — the "design doc §5.3/§6.2" citations in `README.md` and
> `BUILD_CHECKLIST.md` were AI-generated references to a document that was never
> actually produced. `README.md` has been corrected to remove the false citation.

---

## Session 0 — claude.ai (web), date unknown, prior to 2026-08-27

*Planning conversation that produced the design/spec document and the original,
uncompiled application scaffold later handed to Claude Code in Session 2. Reconstructed
from screenshots of the conversation, not an automated transcript pull.*

0a. *(2 reference images attached — UI "vibe" mockups showing a code/QR check-in
    screen and a stats dashboard)*
   ```
   this is the vibe im going for, i want the lecturer to be able to put a code and a
   qr code on the screen and student scan it, they only have to sign up once, which
   they have to enter their student number - like ANSTAN001 then they can just sign
   in again
   ```

0b. *(after Claude generated a "Present attendance system plan" spec document)*
   ```
   its a website. lecturers can set the dates and time for the week, they can choose
   different options lke workshops, lectures or tests or add their own, the lectures
   can see overall attendatnce and stats related to attendance like with graphs and
   stuff and the student will scan the codeon the board itll open the website itll
   then check them in automatically they can then look at their attendance score and
   they can see the days they missed on a calendar, gree circle around if they
   attended, red if they didnt, plain if no class that day. the students should be
   able to query their attendance like if it says they were absent then they can
   click a button enter date and say i was there and then on the lecturer side they
   can update that persons attence if needed.
   ```

0c.
   ```
   use that plan to build the starting project
   ```

---

## Session 1 — 2026-08-27

1. *(07:51)*
   ```
   dotnet restore
   dotnet ef migrations add Init
   dotnet run
   ```

## Session 2 — 2026-08-27

2. *(07:56)*
   ```
   This is an ASP.NET Core 8 Razor Pages attendance app (Identity auth, EF Core + SQLite,
   SignalR, QRCoder). It was hand-written without being compiled — read README.md first
   for what's implemented vs. stubbed, then:

   1. Run `dotnet restore` and `dotnet build`, fix any compile errors (missing usings,
      API mismatches, etc.) without changing the intended behaviour.
   2. Run `dotnet ef migrations add Init` and confirm the app starts with `dotnet run`.
   3. Walk through the flows manually (or write a couple of integration tests) to check:
      register as Lecturer, create a weekly meeting, start it, register as Student and
      check in via the manual code, confirm the board updates and the calendar shows green.
   4. Flag anything in the code that looks like a design decision I should be making
      myself rather than something you should just "fix" — don't silently change
      behaviour like the session-open policy or the enrolment model.

   Don't rewrite the architecture, just get it running and report what you had to fix.
   ```

3. *(08:14)* `i cannot see the website`
4. *(08:17)* `cant be found`
5. *(08:17)* `whats the email`
6. *(08:22)* `can you fix the qr code cause it goes t the error`
7. *(08:30)* `run though everything and fix everything`
8. *(08:37)* `read through buildchecklist` *(referring to `BUILD_CHECKLIST.md`)*
9. *(08:39)* `here`
10. *(19:40)* `it literally shows noting`

## Session 3 — 2026-08-29 (this session)

11. *(06:49)* `i didnt read properly and i was supposed tyo start by pulling https://github.com/community/community`
12. *(06:52)* `2` *(selecting an option from a clarifying question)*
13. *(06:54)* `do it`
14. *(07:25)* `is it still installing`
15. *(07:38)* pasted terminal output showing `gh auth login` failing (`gh` not on PATH)
16. *(07:39)* `i logged in on the browser`
17. *(07:49)* pasted `gh auth status` output confirming login
18. *(07:59)* `uhm` *(reacting to the GitHub Classroom retirement discovery)*
19. *(08:00)* `yeah do that` *(approving creation of a personal fallback repo)*
20. *(08:01)* `ANSTAN - Tanit Ansara` *(student number + name for the README)*
21. *(08:05)* `go through this create a checklist of everything that needs to be done, then check it against what i have and create a list of everything i still need to do` *(with the case study PDF attached)*
22. *(08:11)* `design decisions doc`
23. *(08:16)* `extra was also the design and look like the bubble of students names dropping in`
24. *(08:17)* `comile ai prompt log`
25. *(08:19)* pasted `present-build-prompt (1).md` (the original spec doc from
    Session 0) and said `here`
26. *(08:21)* `nah it just builtit` *(confirming no separate numbered design doc
    ever existed — the "§5.3/§6.2" citations were AI-invented)*
27. *(08:22)* `yes do both` *(requesting both UML artefacts — class diagram and
    sequence diagram)*
28. *(09:11)* `run the app`
29. *(2026-08-30, 12:31)* `okay so there is an issue, the import is only 1 button its
    not every single. look at thatcsv file i updated it needs to be able to take that
    and workout that persons attendace with the calendr. there is also an issue with
    the start session it just says not today`
30. *(12:52)* selected "Auto-create student accounts on import" when asked how to fix
    the import gap
31. *(13:10)* `okay no you are still doing this wrong, 1 button for input past
    attendance not for every session it is overall attendance which has different
    sessions and students dates it ist per session. when a lecturer signs up they
    would input the data they have before they starrted using the app then the
    system will calculate the attendance for each student and they will be able to
    loggin and see their attendance. and if the lecturer says start session the it
    must show the qr code screen`
32. *(2026-08-30, 18:00)* `sign up and sign i doesnt work anymore`
33. *(18:00)* `TAKE OUT THAT IMPORT HISTORY` *(interrupting the sign-in
    investigation to remove the standalone ImportHistory page immediately)*
34. *(18:07)* `oh my word no, please just ask me exactly what i want. i want 1
    single import button where a csv file can be inputted. the file has multiple
    different days attendance`
35. *(18:08)* answered clarifying questions: sign-in was confirmed working (stale
    browser tab), and the import button should sit directly on the landing page,
    not a separate page
36. *(2026-08-30, 18:18)* `Class snapshot — four tiles: overall attendance %,
    sessions held, students tracked, and a "need attention" count that turns red
    when it's non-zero. Students who need attention — this is the alarming-
    attendance list you asked for. It flags anyone under 75% attendance or on a
    live streak of missed sessions, worst offenders first, each showing their %
    and either "missed last N in a row" or their last-attended date. A critical
    badge (red) kicks in under 50% or a 3+ streak; everything else flagged is
    amber. Attendance trend — upgraded from bars to a smooth line/area chart with
    a gradient fill, week by week. Attendance distribution — a donut chart sorting
    the whole class into Excellent / Good / At risk / Critical bands, with counts.
    By session type and by day of week — bar charts to spot whether, say, Fridays
    or Tests are where attendance drops. Per-student table stays at the bottom for
    the full picture, tap any row to expand their session history. make this ask
    questions to make sure of everything before you do it`
37. *(18:19)* answered clarifying questions: all-time window, combined across all
    meetings, Excellent ≥90/Good 75-89/At risk 50-74/Critical <50 band cutoffs,
    category charts by average % rather than headcount
38. *(18:35)* answered follow-up: tighten "needs attention" to require a 2+ session
    miss streak (not just 1+), after being shown it flagged 60% of the class on a
    real 104-student test import
39. *(2026-08-30, 21:07)* `the sign in does not work and make sure each button goes
    to right place`
40. *(21:30)* `install the emil kowaslki design skill the impeccable design skill
    and the taste sill` *(installed three external Claude Code skills — Emil
    Kowalski's emil-design-eng, Paul Bakaus's impeccable, and taste-skill — as
    local tooling under `.claude/skills/`, outside the app's own git repo)*
41. *(21:45)* `yes run it` *(ran the impeccable skill's "polish" pass against
    site.css and the shared layout)*
42. *(22:00)* [attached a reference image of a dark fintech dashboard]
    `this is what i want it too look like, lecturer and student side. plan it out
    ask as many questions needed i want the same lok feel and want cool animations.
    keep the functionality and drop in bubbles but change colors to match picture`
    *(entered plan mode; produced a design plan at
    `C:\Users\Admin\.claude\plans\this-is-what-i-eventual-gadget.md`)*
43. *(22:02)* answered 4 clarifying questions before the plan was finalized: dark
    theme applies everywhere including sign-in/sign-up, the accent/bubble/chart
    palette is derived from the reference image, animation work is scoped to a
    few signature moments (not motion everywhere), and fintech-specific content
    (wallet/app-icon imagery) is adapted as style only, not copied literally
44. *(22:03)* approved the plan via ExitPlanMode, unchanged
45. *(2026-08-30, 22:20)* `can you give me the url`
46. *(22:22)* [attached a reference image of a split-screen "EduTrace" sign-up page]
    `can you redesign the sign in and sign up pages on desktop it is jsut an
    awkwards size it can look something like the picture`
47. *(2026-08-30, 22:45)* `can you go through all the pages and make sure that fit
    properly if they desktop like the overivew should be i parts nit just scroll
    and under eachother`
48. *(2026-08-31, 07:00)* `okay its important that you turn the stats imported into
    usable data like if the student number matches the one in the sheet the data
    for that peson updates everywhere even on student side like caleder needs to
    see where they attended and missed and also add stats like their attendance
    agaisnt the class average or some thing like that`

---

**Total prompts across all sessions: 52** (3 in Session 0, 49 in Sessions 1–3)
