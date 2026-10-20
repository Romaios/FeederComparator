# Feeder kWh Copy Agent Rules

These rules apply to Claude, Codex, and any other coding agent working in this repository.
Background: `C:\Users\USER-01\Documents\BongBillingV2\docs\LESSONS-LEARNED-SBPR-BONGBILLING.md`.

## Priority

- Follow the current user request exactly.
- If a direct user instruction conflicts with this file, the direct user instruction wins.
- Keep edits limited to the files required for the active task.

## Execution

- Edit, build, and verify directly when the request is clear.
- Do not add placeholder code, stubs, TODOs, FIXMEs, or partial implementations.
- No worktrees. Never use `git worktree add`. Work in this checkout.
- Do not create git branches unless the user explicitly asks.
- Batch related edits, build once, verify once. Run the built exe, not `dotnet run`.

## Documentation And Comments

- No inline comments, block comments, XML documentation comments, or docstrings unless the user asks for a specific one.
- No Markdown documentation, summaries, or handoff notes unless the user asks.
- No commented-out code.

## Architecture

- MVP for every screen: `Core/Presenters/<Feature>/I<Feature>View.cs` + `<Feature>Presenter.cs`, `App/Forms/<Feature>/<Feature>View.cs` + `.Designer.cs`.
- `FeederCopy.Core` must never reference WinForms. Presenters talk to views only through the interface.
- Business math lives in `Core/Models/<Feature>/`. SQL lives only in `Core/Services/Data`.
- Views are passive: relay control events to the presenter, display what the presenter gives them. No logic, no SQL.
- Layout lives in `.Designer.cs`. Code-behind stays thin.
- Every view has its own `ApplyTheme()` that uses `Utils/AppTheme` `Style*()` helpers. No view styles another view's controls.
- Workflows stay inside the main window (embedded views). Only file pickers and yes/no confirmations may pop up.
- Hard limit: no file past 1,000 lines.

## Data And Config

- The As-Billed files and feeder list files are read-only inputs. Never modify them.
- The schema lives in numbered, dated scripts in `docs/sql/` (`YYYY-MM-DD-NN-name.sql`). On start the program applies, in order, any script the database has not run yet (tracked by `PRAGMA user_version`). Never edit a script that has shipped; add a new one.
- Parameterized queries only.
- Config lives in `settings.json` via `SettingsStore`. No hardcoded business data (column names, feeder names, bus IDs). Never edit the user's runtime `settings.json` directly; tell the user what to change in the Settings tab.
- If config is invalid, surface the problem in the UI. Never silently fall back.

## Error Handling

- Do not swallow exceptions. Log with context (`IErrorLog`) and show a concise message.

## UI And UX

- Use `TableLayoutPanel`/docking for layout. No literal pixel positioning in code-behind.
- Every control must be fully functional. Handle empty, loading, and error states.
- Final visual approval comes from the user seeing the app; do not run automated visual-inspection tests.

## Versioning

- X.Y.Z: X = overhaul, Y = one major or 5+ minor changes, Z = fixes. Version lives in `src/FeederCopy.App/FeederCopy.App.csproj`.

## Completion Standard

- Do not claim work is complete without building and running the tests.
- Keep explanations in chat short and plain.
