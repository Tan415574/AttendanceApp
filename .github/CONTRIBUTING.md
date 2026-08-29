# Contributing

This is a coursework project for INF3003W. If you're working on it alongside
others (or reviewing your own future changes):

1. Create a branch per feature/fix rather than committing straight to `main`.
2. Run `dotnet build` before opening a PR — see the README's "Known rough
   edges" section for things that are expected to need fixing.
3. Add an EF Core migration (`dotnet ef migrations add <Name>`) whenever you
   change anything under `Models/` or `Data/ApplicationDbContext.cs`.
4. Don't commit `attendance.db`, `bin/`, or `obj/` — they're already in
   `.gitignore`.
5. Use the issue templates to track anything from the README's "Not built"
   list (spreadsheet import, course/module scoping, deployment config).
