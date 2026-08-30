<!-- ANSTAN - Tanit Ansara -->

# Dark fintech-style reskin of AttendanceApp

> Copied into the repo from Claude Code's plan-mode output for durability — this is
> the plan approved before implementation began. One deviation made during execution,
> noted here rather than silently: the "Need attention" snapshot tile was **not** given
> the purple hero-gradient treatment as originally planned. That tile's whole purpose is
> to read red when non-zero (an intentional alert signal, see `DESIGN_DECISIONS.md`
> §7's Overview dashboard section); a decorative purple gradient would have undermined
> that. The purple hero gradient went to "Students tracked" instead — a neutral info
> tile — and "Need attention" kept its plain-card/red-when-nonzero treatment unchanged.

## Context

The current UI is a light pastel-green "health app" look (from `ORIGINAL_SPEC.md`). The
user shared a reference image of a dark fintech dashboard (ethereal) — near-black
background, glassy rounded cards, two signature mint-green and purple gradient hero
cards, colorful circular accent chips, a pill-shaped segmented nav, a donut chart and a
bar chart in matching saturated colors — and wants the whole app (lecturer and student
sides, including sign-in/sign-up) reskinned to match that look and feel, with a few
polished "signature" animations. Functionality is unchanged throughout, including the
matter.js physics drop-in bubble board — only its color palette changes to match the
new system. This is a visual reskin, not a rebuild: every route, form, and behavior
already in the app stays exactly as-is.

Confirmed with the user before planning: dark theme applies everywhere (not just
post-login), the new bubble/chart/accent palette is derived from the reference image,
animation work is scoped to a few signature moments rather than motion everywhere, and
fintech-specific content (Discord/Spotify icons, "My wallet") is adapted as *style*
only — no literal unrelated content gets copied in.

## Design system (site.css `:root` tokens — full replacement)

| Token | Old (light) | New (dark) |
|---|---|---|
| `--bg` | `#eef3ea` | `#0c0c14` (near-black navy) |
| `--card` | `#ffffff` | `#17171f` (glass surface), with a `1px solid rgba(255,255,255,0.06)` hairline border — dark UIs read as "cards" via border + subtle glow, not drop shadow |
| `--text` | `#1c1c1c` | `#f2f2f6` |
| `--muted` | `#5c6459` | `#9d9db0` |
| `--accent` / `--accent-dark` | greens | keep a green identity (`#8ef07f` / `#5fce63`) so the app doesn't lose its brand color, but only as *one* accent among the new multi-color set below |
| `--red` | `#e8641c` | `#ff7a6b` (coral, reads correctly on dark) |
| `--radius` | `18px` | `22px` (reference's cards are noticeably rounder) |

New tokens added:
- `--hero-gradient-1`: `linear-gradient(135deg, #c9f27a, #6fcf6f)` — mint/lime, used as the join-code/QR hero panel and the "Overall attendance" snapshot tile
- `--hero-gradient-2`: `linear-gradient(135deg, #b39dfb, #7b6fe8)` — purple/lavender, used as the "Students tracked" snapshot tile
- `--accent-purple: #9b8cfb`, `--accent-pink: #f17fb0`, `--accent-blue: #5ac8fa`, `--accent-teal: #3fd9c7`, `--accent-amber: #f5b942`, `--accent-coral: #ff7a6b`, `--accent-indigo: #6c7bf0`, plus the existing green — this 8-color set is the new palette for: bubble avatars (`board.js`), the donut chart segments, and any small color-chip UI
- Card shadow → glow: replaced `box-shadow: 0 ... rgba(28,28,28,...)` with a soft dark-appropriate shadow (`0 8px 30px rgba(0,0,0,0.35)`) plus the hairline border above

## Component changes, by file

**`wwwroot/css/site.css`** (full token rewrite + component updates)
- `:root` tokens per table above
- `.card` → dark surface, hairline border, new shadow, `--radius`
- `.btn` → primary stays green (brand continuity), hover/press glow instead of light-shadow; `.btn.secondary` becomes a dark ghost/outline button; `.btn.danger` uses `--accent-coral`
- `.topbar` → near-black glass (blurred), no bottom box-shadow line (hairline border instead)
- New `.nav-pill` / `.nav-pill a` styles: a floating rounded segmented control replacing the current plain text nav links, with an active-state pill background
- `.snapshot-tile` → "Overall attendance" gets `--hero-gradient-1`, "Students tracked" gets `--hero-gradient-2`; "Sessions held" and "Need attention" stay on the standard dark surface (see the deviation note above)
- `.pill`, `.badge` → recolor from the new accent set (lecture→blue, workshop→green, test→coral; critical badge→coral, amber badge→amber)
- `.cal-cell` present/absent → translucent green/coral from the new tokens
- `.join-code` panel → `--hero-gradient-1`; `.board-canvas-holder` → dark glass card so falling bubbles read clearly against it
- `.student-table`, `.attention-row`, `.meeting-row` → dark hover states
- Signature entrance animation added as a shared keyframe/class

**`Pages/_Layout.cshtml`**
- Nav restructured into a pill/segmented control, current page's link marked active via `Context.Request.Path.StartsWithSegments(...)`
- Sign out stays a small text control on the right — no literal bell/search/avatar icons invented, per the "adapt style, not literal content" decision

**`wwwroot/js/board.js`** + **`Services/AvatarAssigner.cs`**
- Replaced the 12-avatar palette with the new 8-color accent set (cycling), keeping shapes untouched — a data-only change, kept in sync between the two files per the existing code comment

**`Pages/Lecturer/Overview.cshtml`**
- Recolored all 4 Chart.js configs to the new accent set, dark-mode axis/legend colors
- Added a vanilla-JS count-up animation on the 4 snapshot tiles, respecting `prefers-reduced-motion`
- Tuned each chart's entrance animation easing

**Inline hardcoded hex colors — swept and converted to tokens.**
9 files had literal hex in `style="..."` attributes bypassing the CSS variables:
`Login.cshtml`, `Lecturer/Meetings/Create.cshtml`, `Lecturer/Meetings/Import.cshtml`,
`Lecturer/Meetings/Index.cshtml`, `Lecturer/Board.cshtml`, `Lecturer/Overview.cshtml`,
`Lecturer/Queries.cshtml`, `Student/Attendance.cshtml`, `Student/Query.cshtml`. All
converted to the matching CSS variable so the dark-theme rewrite actually takes effect
everywhere.

## Animation plan (scoped to signature moments)

1. **Card entrance**: staggered fade/slide-up on page load.
2. **Snapshot tiles count up** from 0 on the Overview page load.
3. **Chart entrance easing** tuned on all 4 Overview charts.
4. Existing hover/press micro-interactions retuned for the dark palette.

Not in scope: page-to-page transitions, skeleton loading states, parallax, or
animating every list item individually.

## Explicit non-goals

- No literal fintech content (no wallet/app-icon imagery) — style only.
- No functional changes anywhere.
- No new JS dependencies.

## Verification performed

- `dotnet build` after each step.
- Full Playwright audit (`test_full_audit.js`) re-run post-reskin: 24/24 checks pass,
  zero regressions, zero console/HTTP errors.
- Visual screenshots of every major page in both roles, compared against the reference
  image's color/shape language.
