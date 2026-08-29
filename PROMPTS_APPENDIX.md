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

---

**Total prompts across all sessions: 31** (3 in Session 0, 28 in Sessions 1–3)
