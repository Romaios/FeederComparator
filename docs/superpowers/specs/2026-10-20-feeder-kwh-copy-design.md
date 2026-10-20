# Feeder kWh Copy: Design

Date: 2026-10-20 · Status: draft for the owner's approval

## 1. Intent

Today, filling the feeder lists is done by hand in Excel. Each feeder (for example New York, Washington, Brooklyn) has its own list of consumers, grouped by bus ID. When the branch reports a new consumer, someone has to find that bus's block, insert a row inside it, type the bus ID and consumer ID, renumber, and then copy each consumer's kWh from the monthly As-Billed file. Adding the row at the bottom (row 1190) instead of inside its bus block (rows 21–30) breaks the grouping.

This program keeps the feeder lists for you and does the copy-and-paste:

1. **Once:** import each feeder's consumer list. The program saves it.
2. **Each month:** type in the new consumers the branch reported. Each one lands inside its bus's block.
3. **Each month:** load that month's As-Billed file. The program copies each consumer's KWHUSED.
4. **Each month:** export the finished feeder sheets to Excel.

The user is a colleague of the owner. The owner relays requests.

## 2. Goals

- Feeder lists are saved and reused month after month. Importing them again is optional.
- A new consumer is always inserted at the end of its bus's block, never at the bottom of the feeder (unless the bus is new or unknown, see §6).
- kWh is copied by matching the feeder's Consumer ID to the As-Billed ACCOUNTNUMBER.
- Anything the program can't copy cleanly is highlighted and listed, never guessed.
- The output matches the layout in §7.

## 3. Non-goals (for now)

- Connecting to the billing server. Planned for later. The As-Billed source sits behind an interface, so a server reader can replace the file reader without rework.
- Keeping kWh history or doing any monitoring or charts. Each run is a single copy from a single month.
- Reordering consumers inside a bus block, or reordering the bus blocks.
- Reading the feeder list from the branch automatically. New consumers are typed in.

## 4. Anti-goals (failures even if it "works")

- Changing the As-Billed file in any way. It is a read-only input.
- Silently picking one value when an account number appears twice in the As-Billed file.
- Silently writing 0 for a consumer who isn't in the As-Billed file.
- Losing or reshuffling the existing order of a feeder when a consumer is added.
- Hardcoding feeder names, bus IDs or column names.

## 5. Data

### 5.1 Feeder list (import file)

A plain list of the consumers on one feeder. Columns: **Count, Bus ID, Consumer ID**.

- Formats: `.xlsx`, `.csv`, and SpreadsheetML `.xls` (same readers as the Jonnel project).
- Columns are found by header name, case-insensitively and in any order. The header names come from Settings, with defaults `Count`, `Bus ID`, `Consumer ID`.
- **Count is ignored on import, and may be missing.** The program numbers rows itself.
- Bus IDs match case-insensitively after trimming. The first spelling seen is kept. *(My call.)*
- Row order in the file is the feeder's order. Bus IDs are **text** (for example `CONSjunc14Se`) and are never sorted. The order of first appearance defines the order of the bus blocks.
- The feeder name is typed by the user when importing (pre-filled from the file name).
- Blank rows are skipped. A row with a Consumer ID but no Bus ID imports as Unknown bus (§6).

### 5.2 As-Billed file (monthly)

The same export used in the Jonnel project: SpreadsheetML `.xls`, `.xlsx` or `.csv`. About 113k rows, one month per file. Only three columns are used: `ACCOUNTNUMBER`, `KWHUSED` and `BILLMONTH`, with names from Settings. Both the Jan–Mar and Apr+ layouts work, because the columns are found by name.

The month comes from BILLMONTH. As in the Jonnel project, the user confirms the month before the copy runs.

### 5.3 Storage

SQLite file next to the exe (`feeders.db`). Schema lives in numbered scripts in `docs/sql/` and is applied on start, tracked by `PRAGMA user_version`, the same way as in the Jonnel project.

- `Feeders (Id, Name UNIQUE, CreatedAt)`
- `FeederConsumers (Id, FeederId, BusId NULL, ConsumerId, Position)`. `Position` gives the order inside the feeder. `ConsumerId` is unique across **all** feeders.

kWh values are not stored. They live only in the current copy run until exported.

## 6. Behavior

### 6.1 Importing and updating a feeder

- **New feeder:** pick the file, type or confirm the name, preview the row count, bus count and any problems, then save.
- **Update an existing feeder from a newer list:** the newer list replaces the saved one. Before saving, a preview shows how many consumers will be added, removed and moved to a different bus. *(My call: replace, with a preview, rather than merge.)*
- A Consumer ID that already belongs to a **different** feeder blocks the import, with a message naming that feeder and bus.
- A Consumer ID repeated inside the same file blocks the import, with the row numbers listed.

### 6.2 Adding a consumer by hand

A form inside the main window with these fields: Feeder (pick), Bus ID (pick from that feeder's buses, type a new one, or leave blank), and Consumer ID.

| Case | Result |
|---|---|
| Bus ID exists in that feeder | Inserted right after the last consumer with that bus ID. |
| Bus ID is new to that feeder | A new bus block starts at the bottom, above the Unknown block. |
| Bus ID left blank | Goes into the Unknown block at the very bottom. |
| Consumer ID already in any feeder | Refused. The message names the feeder and bus it's already under. |

### 6.3 Editing consumers

From a feeder's consumer list:

- **Set or change the bus ID.** The consumer moves to the end of that bus's block, using the same rules as §6.2. This is how an Unknown consumer gets its bus later.
- **Move to another feeder.** Uses the same rules as §6.2.
- **Remove,** after a yes/no confirmation.
- **Rename or delete a feeder.** Delete asks for confirmation.

Count always renumbers 1…n from top to bottom.

### 6.4 Copying kWh

1. Pick the As-Billed file and confirm the month.
2. The program reads it in the background with a progress display, and indexes ACCOUNTNUMBER → KWHUSED.
3. For each consumer in every feeder:

| Case | kWh cell | Listed in summary |
|---|---|---|
| Found once | The value, as billed (zero and negative copied as they are) | No |
| Not in As-Billed | Blank, highlighted | Yes: "Not in As-Billed" |
| Account number appears 2+ times | Blank, highlighted | Yes: "Appears N times in As-Billed" |

4. The screen shows each feeder's totals: consumers, copied, not found, duplicates, kWh total. Below that is the full problem list, which can be saved as a text file.

Matching: Consumer ID and ACCOUNTNUMBER are compared as text after trimming spaces. *(My call: no other normalization, such as dropping leading zeros or dashes, unless the real data shows it's needed.)*

Accounts that are in the As-Billed file but in no feeder are not reported. New consumers come from the branch, not from the As-Billed file. *(My call.)*

### 6.5 Export

The user picks the output style each time, and the last choice is remembered in Settings:

- **One workbook:** one `.xlsx`, one sheet per feeder (sheet name = feeder name, cut to Excel's 31-character limit and cleaned of invalid characters).
- **Separate files:** one `.xlsx` per feeder, saved into a chosen folder, named `<Feeder> - <Mon YYYY>.xlsx`.

Only enabled after a copy run.

## 7. Output layout (each feeder sheet)

```
           New York Feeder                      <- row 1: feeder name as typed, merged across A–D, bold
 Count | Bus ID       | Consumer ID | kWh       <- row 2: headers (kWh header shows the month, e.g. "kWh (Jul 2026)")
   1   | CONSjunc14Se | 0101-0001   | 152.0
   2   | CONSjunc14Se | 0101-0007   |  88.5
   3   | BUS22        | 0102-0003   | 230.0
  ...
  41   | Unknown      | 0109-0044   |           <- Unknown block last; blank kWh highlighted
```

- Consumer ID is written as text, so leading zeros stay.
- kWh is numeric. Highlighted blanks use a light fill.
- Header row frozen. Columns auto-fit.

## 8. Screens

Everything is embedded in one main window, with tabs, as in the Jonnel app. Only file pickers and yes/no confirmations pop up.

- **Feeders:** the feeder list on the left, with Import list, Update from list, Rename and Delete. The selected feeder's consumers are on the right in a grid (Count, Bus ID, Consumer ID), with Add consumer, Set bus ID, Move to feeder and Remove. The add form sits above the grid. Search by Consumer ID.
- **Copy kWh:** pick the As-Billed file, confirm the month, run. Shows the per-feeder totals grid, the problem list, the output style switch and Export.
- **Settings:** column names for the feeder list and the As-Billed file, and the export style default. The settings are validated, and problems are shown in the UI.

Empty, loading and error states are handled on every screen. For example, with no feeders yet, the Feeders tab says to import one first.

## 9. How it gets built

These choices follow the Jonnel project and the SBPR lessons file. They are my calls, made on the owner's instruction to use those projects' rules.

- C# / .NET 10 WinForms. Solution: `src/<Name>.Core` (no WinForms), `src/<Name>.App`, `tests/<Name>.Tests` (xUnit).
- MVP for every screen: the presenter and `I<Feature>View` live in Core; a passive view with a `.Designer.cs` lives in App.
- `AppTheme` with per-view `ApplyTheme()`, the same look as the Jonnel app.
- Packages, same versions as Jonnel: Microsoft.Data.Sqlite, Dapper, ClosedXML, CsvHelper.
- The As-Billed readers (`SpreadsheetMlReader`, `XlsxReader`, `CsvFileReader`, reader factory, `HeaderMap`) are copied from the Jonnel project and adapted. The source sits behind an interface (`IKwhSource`), so the future server reader slots in.
- Pure logic in calculators with unit tests: block insertion (§6.2), renumbering, the update preview diff (§6.1), the copy matching (§6.4) and sheet-name cleaning.
- `settings.json` via `SettingsStore`. Parameterized SQL only. Errors are logged to `logs/` and shown in short messages.
- `AGENTS.md` copied from the Jonnel project and adjusted. No code comments. No file over 1,000 lines. Version starts at 1.0.0. Git repo in this folder, with no branches and no worktrees.
- Tests use fictional data only. Real As-Billed files contain real names and never go in the repo.

## 10. Left to the builder

- Exact grid columns, button labels and layout spacing, following the Jonnel conventions.
- The app name. *(My call: "Feeder kWh Copy" unless the owner names it.)*
- Progress-report frequency and background threading details.

## 11. Revision 2026-10-20: consumer status and monthly history

Why: the feeder data still lists consumers who were disconnected long ago. Those consumers are what caused the duplicate-ID problems across sheets. The user will add a status to the workbook. Status stays a manual stand-in until the program connects to the billing server, as the CPD monitoring program does.

### 11.1 Status in the workbook

- New optional column **Status**, with the name set in Settings (default `Status`). `1` means active and `0` means not active. A blank value counts as active. Any other value is a problem that blocks saving: `Sheet X row N: status 'abc' must be 1 or 0.`
- A sheet with no Status column imports everyone as active.
- Inactive consumers stay in their feeder as stand-ins and are shown greyed.
- **Same ID on more than one row (any sheets):**
  - Exactly one copy is active: keep it and drop the inactive copies, with a note.
  - Two or more copies are active: a problem that blocks saving. The messages are the ones from §6.1.
  - No copy is active: keep the first one, with a note.

### 11.2 Copy kWh statuses

| Status | In As-Billed | kWh | Export Status | Problem |
|---|---|---|---|---|
| Active | once | copied | Active | – |
| Active | missing | blank | Missing reading | yes |
| Inactive | missing | blank | Inactive | – |
| Inactive | once | copied | Inactive but billed | yes |
| any | 2+ times | blank | Duplicate in As-Billed | yes |
| any | unreadable kWh | blank | kWh unreadable | yes |

- "Missing reading" replaces the old "Not in As-Billed" wording.
- The per-feeder totals add Active, Inactive, Missing reading and Inactive but billed.
- **Each Copy kWh run is saved by month** (consumer ID, month, kWh, status). Running the same month again replaces that month. This reverses the §3 non-goal "no kWh history".

### 11.3 Export

The columns become `Count | Bus ID | Consumer ID | Status | kWh (Mon YYYY)`. Rows with a problem are highlighted.

### 11.4 Feeders tab

The grid gains a Status column (Active or Inactive), with inactive rows greyed. The selected-consumer bar gains **Set active** and **Set inactive**. Changing a bus ID or moving a consumer keeps its status. New consumers start active.

### 11.5 New Consumers tab (by year)

```
Feeder [All ▾]  Status [All ▾]  Year [2026 ▾]  Find [______]
Feeder | Bus ID | Consumer ID | Status | Jan | Feb | … | Dec | Total
Selected: 0900001 · Bacon · Active      [Set active] [Set inactive]
```

- Covers every consumer in every feeder, in feeder then position order.
- Each month cell shows the saved kWh. When there's no kWh, it shows the saved status for that month ("Missing", "Inactive"). It's blank when that month was never copied.
- Total is the sum of that year's kWh.
- The year list holds the years with saved readings, plus the current year. It defaults to the latest one.
- Changes made on the Feeders tab or by Copy kWh refresh this tab.
