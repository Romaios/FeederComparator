# Consumer Status and Monthly History Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the active/inactive status from the workbook, status-aware kWh copy with saved monthly readings, a Status column in the export and on the Feeders tab, and a new Consumers tab showing one year at a time.

**Architecture:** Existing FeederCopy MVP. Status is a field on `ConsumerEntry`, defaulting to active. Readings live in a new `Readings` table behind `IReadingService`. The Consumers tab is a new MVP screen whose grid rows come from a pure calculator.

**Tech Stack:** As before (.NET 10 WinForms, SQLite/Dapper, ClosedXML, xUnit).

**Spec:** `docs/superpowers/specs/2026-10-20-feeder-kwh-copy-design.md` §11

## Global Constraints

- Every `AGENTS.md` rule applies: no comments, MVP, Core never references WinForms, parameterized SQL, and no file over 1,000 lines.
- Status labels are exactly: `Active`, `Inactive`, `Missing reading`, `Inactive but billed`, `Duplicate in As-Billed`, `kWh unreadable`.
- `ConsumerEntry(string? BusId, string ConsumerId, bool Active = true)`. Every list edit keeps `Active`.
- Schema change goes in `docs/sql/2026-10-20-02-add-status-and-readings.sql`. The existing script is never edited.
- Version goes to `1.3.0`.

## Review Focus

1. **An existing `feeders.db` from 1.2.x.** Expect the upgrade to keep every consumer, all set active. Tested in Task 1 (DatabaseTests).
2. **An existing `settings.json` without a Status property.** Expect it to load with Status = "Status" and no warning banner. Tested in Task 1 (SettingsStoreTests).
3. **Status cells stored as numbers in xlsx.** Expect `1` or `0` to read correctly (reader gives "1"/"0"). Tested in Task 2.
4. **Copying the same month twice.** Expect that month's readings replaced, not doubled. Tested in Task 3.
5. **Set bus ID or Move on an inactive consumer.** Expect it to stay inactive. Tested in Task 4.

---

### Task 1: Schema, status on ConsumerEntry, settings

- Files:
  - `docs/sql/2026-10-20-02-add-status-and-readings.sql`
  - `Models/Feeders/FeederModels.cs`
  - `Models/Feeders/FeederLayout.cs`
  - `Services/Data/FeederService.cs` + interface
  - `Models/Settings/AppSettings.cs`, `SettingsValidator.cs`
  - Tests
- The schema script:
  ```sql
  ALTER TABLE FeederConsumers ADD COLUMN Active INTEGER NOT NULL DEFAULT 1;
  CREATE TABLE Readings (ConsumerId TEXT NOT NULL, BillMonth TEXT NOT NULL, Kwh REAL NULL, Status TEXT NOT NULL, PRIMARY KEY (ConsumerId, BillMonth));
  CREATE INDEX IX_Readings_BillMonth ON Readings(BillMonth);
  ```
- `ConsumerRow` gains `bool Active` and `string StatusLabel`.
- `FeederLayout.SetBus` keeps Active, and a new `FeederLayout.Add(entries, ConsumerEntry entry)` keeps the passed entry's Active. Move uses it.
- The service reads and writes `Active`. Add `void SetActive(string consumerId, bool active)` and `IReadOnlyList<(string FeederName, ConsumerEntry Entry)> GetAllConsumers()` (feeder name, then position).
- `FeederColumns.Status` is non-required, defaults to `"Status"`, and may be blank (blank means no status column). The validator rejects a Status name equal to the Bus ID or Consumer ID name.
- Tests:
  - Upgrade from schema 1 keeps rows and makes them active.
  - `SetActive` and `GetAllConsumers`.
  - `SetBus` keeps Active.
  - Old settings JSON without Status loads with no problem.
  - Validator rejects a duplicate Status name.

### Task 2: Import with status

- Files: `Services/Import/FeederListParser.cs`, tests.
- The Status column is optional, found the same way as the other headers (`HeaderIndex.Find` returns `int?`).
- Values: `1` is active, `0` is inactive, blank is active. Anything else gives the problem `Sheet X row N: status 'v' must be 1 or 0.`
- Duplicate resolution runs across the whole workbook, per spec §11.1. Note texts:
  - `Consumer ID c1: kept the active one on Bacon; dropped the inactive copy on Sorsogon.`
  - `Consumer ID c1 is inactive on Abuyog and Bacon; kept the one on Abuyog.`
  - Same sheet: `Sheet X rows 3, 7` style.
- Tests: one per rule, plus numeric `1`/`0` cells.

### Task 3: Copy statuses, saved readings, report, export

- Files:
  - `Models/Copy/CopyModels.cs`, `KwhCopier.cs`, `CopyReport.cs`
  - New `Models/Readings/ReadingModels.cs` (`ReadingRecord(ConsumerId, BillMonth, double? Kwh, string Status)`)
  - New `Services/Data/IReadingService.cs` / `ReadingService.cs`: `SaveMonth(BillMonth, IReadOnlyList<ReadingRecord>)`, `GetYears()`, `GetYear(int year)`
  - `Services/Export/FeederExporter.cs`
  - `Presenters/Copy/CopyPresenter.cs`
  - `App/Forms/Copy/CopyView*`
- `FilledRow` gains `string Status`.
- `FeederTotals(Name, Consumers, Active, Inactive, Copied, MissingReading, InactiveButBilled, Duplicates, Unreadable, TotalKwh)`.
- The copy rules follow the §11.2 table. Problem text equals the status label, except duplicates (`Appears N times in As-Billed`) and unreadable (`kWh could not be read in As-Billed (row N)`).
- `CopyPresenter` takes `IReadingService`. After a successful copy it saves the month and raises `event Action? ReadingsSaved`.
- The export column order is `Count | Bus ID | Consumer ID | Status | kWh (Mon YYYY)`.
- Tests:
  - All six rows of the §11.2 table.
  - The new totals.
  - Report line `F: 5 consumers (4 active, 1 inactive), 3 copied, 1 missing reading, 0 inactive but billed, 0 duplicates, 0 unreadable, 15.00 kWh`.
  - Saving the same month twice replaces it.
  - Export header and status cell.
  - The presenter saves readings and raises `ReadingsSaved`.

### Task 4: Feeders tab status

- Files: `Presenters/Feeders/*`, `App/Forms/Feeders/*`, tests.
- `FeedersPresenter.OnSetActiveClicked(string consumerId, bool active)` calls `SetActive`, reloads, reselects the consumer, and raises `FeedersChanged`.
- The grid gets a Status column with inactive rows greyed. The bar gets **Set active** and **Set inactive** buttons. The preview line counts inactive consumers: `Abuyog — new feeder · 412 consumers (3 inactive) · 38 buses`.
- Tests:
  - Set inactive persists.
  - Move keeps an inactive consumer inactive.
  - The preview line shows the inactive count.

### Task 5: Consumers tab by year

- Files:
  - `Models/Consumers/ConsumerYear.cs` (row model + `ConsumerYearCalculator.Build(consumers, readings, year, filter)`)
  - `Presenters/Consumers/IConsumersView.cs`, `ConsumersPresenter.cs`
  - `App/Forms/Consumers/ConsumersView(.Designer).cs`
  - `MainForm` (new tab "Consumers" between Copy kWh and Settings)
  - Tests
- `ConsumerYearRow(Feeder, BusLabel, ConsumerId, Active, StatusLabel, string[12] Months, double Total)`. A month cell is the kWh formatted `N2`, or `Missing`/`Inactive`/`Inactive*`/`Duplicate`/`Unreadable` when the kWh is null, or blank when there is no reading.
- `ConsumerFilter(string? Feeder, bool? Active, string? Search)`. Search matches the consumer ID with a contains match after trimming.
- `ConsumersPresenter`:
  - `Load()`
  - `OnFilterChanged(string? feeder, bool? active, int year, string? search)`
  - `OnSetActiveClicked(string consumerId, bool active)`, which raises `StatusChanged` so MainForm refreshes Feeders
  - `Refresh()`, called on `FeedersChanged` and `ReadingsSaved`
- Tests: the calculator (cells, total, filters, year), the presenter (years default to the latest, set status).

### Task 6: Verify and ship

- Run a full build (0 warnings) and the tests.
- Run the scratch view check, including the Consumers view.
- Run an end-to-end: import a workbook with status, copy two months, check the readings by year.
- Smoke-start the app, bump to 1.3.0, and commit.
