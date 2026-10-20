# Feeder kWh Copy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an offline WinForms program. It keeps each feeder's consumer list grouped by bus ID, inserts new consumers inside their bus block, copies each consumer's kWh from a monthly As-Billed file, and exports the filled feeder sheets to Excel.

**Architecture:** It follows the same structure as the Jonnel project (`C:\Users\USER-01\Documents\JonnelProject`). `FeederCopy.Core` (net10.0, no WinForms) holds models, pure calculators, SQLite services and MVP presenters. `FeederCopy.App` (net10.0-windows) holds passive views with Designer files. `FeederCopy.Tests` (xUnit) references Core only. Feeder lists are saved in SQLite. kWh values are never stored; they only exist in the current copy run.

**Tech Stack:** C# / .NET 10, WinForms, Microsoft.Data.Sqlite 10.0.12, Dapper 2.1.89, ClosedXML 0.105.1, CsvHelper 33.1.0, xUnit 2.9.3.

**Spec:** `docs/superpowers/specs/2026-10-20-feeder-kwh-copy-design.md`

## Global Constraints

- Every rule in Jonnel's `AGENTS.md` applies. It is copied into this repo in Task 1.
- No code comments of any kind, no TODOs or stubs, and no commented-out code.
- No file over 1,000 lines.
- Core never references `System.Windows.Forms`. Presenters talk to views only through `I<Feature>View`.
- Parameterized SQL only. SQL lives only in `Core/Services/Data`.
- The As-Billed file and the feeder list files are read-only inputs.
- No hardcoded column names. They come from `settings.json` through `SettingsStore`.
- Bus IDs are text (for example `CONSjunc14Se`) and are never sorted. Bus IDs match case-insensitively after trimming, and the first spelling seen is kept.
- Consumer ID and ACCOUNTNUMBER match as text after trimming, using an ordinal comparison.
- The "Unknown" bus label is the exact text `Unknown`, and the Unknown block is always last in a feeder.
- Every view is embedded in the main window. Only file and folder pickers and yes/no confirmations pop up.
- The data folder is the exe folder (`AppContext.BaseDirectory`). Files: `feeders.db`, `settings.json`, `logs/`.
- Version `1.0.0` lives in `src/FeederCopy.App/FeederCopy.App.csproj`. AssemblyName `FeederCopy`. Product name "Feeder kWh Copy".
- Tests use fictional data only. Real As-Billed files never enter the repo.
- Commit message trailer: `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. No branches and no worktrees.

## Review Focus

1. **A file open in Excel (locked)** when importing a feeder list or reading the As-Billed file. Expect a fatal message telling the user to close it in Excel, not a crash. Tested in Tasks 4 and 5.
2. **Consumer IDs stored as numbers in `.xlsx`.** Expect a numeric cell `1010001` to read as `"1010001"` (never `"1010001.0"`), and a text cell `"0101-0001"` to stay as written. Tested in Task 4.
3. **Stray spaces around IDs.** Expect a feeder Consumer ID `" 0101-0001 "` to match an As-Billed ACCOUNTNUMBER `"0101-0001"`. Tested in Task 6.
4. **Feeder names that Excel can't use as sheet names.** Names with `/ \ ? * [ ] :`, names over 31 characters, and two names that clean down to the same sheet name. Expect valid, unique sheet names. Tested in Task 7.
5. **Re-exporting in separate-files mode into a folder that already has those files.** Expect one yes/no confirmation listing the count, and no files written on "No". Tested in Task 11.

---

## File Structure

```
FeederCopy.sln
AGENTS.md
.gitignore
docs/sql/2026-10-20-01-create-feeder-tables.sql
src/FeederCopy.Core/
  FeederCopy.Core.csproj
  Models/Billing/BillMonth.cs                    copied from Jonnel unchanged (namespace only)
  Models/Feeders/FeederModels.cs                 ConsumerEntry, FeederSummary, ConsumerLocation, ConsumerRow, BusLabels
  Models/Feeders/FeederLayout.cs                 bus-block ordering, add/remove/set-bus
  Models/Feeders/FeederRules.cs                  validation texts
  Models/Feeders/FeederDiff.cs                   update preview counts
  Models/Feeders/FeederPreview.cs                PreviewMode, FeederPreview
  Models/Import/ImportModels.cs                  RowError, ParsedFeederList
  Models/Copy/KwhSnapshot.cs                     AsBilledEntry, KwhSnapshot
  Models/Copy/CopyModels.cs                      FeederSource, FilledRow, FilledFeeder, FeederTotals, CopyProblem, CopyResult
  Models/Copy/KwhCopier.cs                       matching + totals
  Models/Copy/CopyReport.cs                      problem list text
  Models/Copy/ExportNames.cs                     sheet and file names
  Models/Settings/AppSettings.cs                 ExportStyle, FeederColumns, AsBilledColumns, AppSettings, SettingsLoadResult
  Models/Settings/SettingsValidator.cs
  Services/Data/Database.cs                      copied from Jonnel, adapted
  Services/Data/IFeederService.cs, FeederService.cs
  Services/Import/ITableReader.cs, TableReaderFactory.cs, SpreadsheetMlReader.cs, XlsxReader.cs, CsvFileReader.cs
  Services/Import/HeaderIndex.cs
  Services/Import/FeederListParser.cs
  Services/Import/IAsBilledReader.cs, AsBilledReader.cs
  Services/Export/IFeederExporter.cs, FeederExporter.cs
  Services/Settings/ISettingsStore.cs, SettingsStore.cs
  Services/Logging/IErrorLog.cs, FileErrorLog.cs  copied from Jonnel
  Presenters/Settings/ISettingsView.cs, SettingsPresenter.cs
  Presenters/Feeders/IFeedersView.cs, FeedersPresenter.cs
  Presenters/Copy/ICopyView.cs, CopyPresenter.cs
src/FeederCopy.App/
  FeederCopy.App.csproj, Program.cs, AppServices.cs
  Utils/AppTheme.cs, Utils/Dialogs.cs
  Forms/Shell/MainForm(.Designer).cs
  Forms/Feeders/FeedersView(.Designer).cs
  Forms/Copy/CopyView(.Designer).cs
  Forms/Settings/SettingsView(.Designer).cs
tests/FeederCopy.Tests/
  FeederCopy.Tests.csproj, GlobalUsings.cs
  Support/TestFiles.cs, Support/TempDatabase.cs, Support/Fakes.cs
  Models/*Tests.cs, Services/*Tests.cs, Presenters/*Tests.cs
```

Namespaces are block-scoped and follow the folders, for example `FeederCopy.Core.Models.Feeders`. View conventions (control prefixes, `Attach(presenter)`, `_populating` flag, Designer nullable rules) follow Jonnel `docs/HANDOFF.md` §12.

---

### Task 1: Solution, rules, logging and database

**Files:**
- Create: `FeederCopy.sln`, `src/FeederCopy.Core/FeederCopy.Core.csproj`, `tests/FeederCopy.Tests/FeederCopy.Tests.csproj`, `tests/FeederCopy.Tests/GlobalUsings.cs`
- Create: `AGENTS.md`, `.gitignore`
- Create: `docs/sql/2026-10-20-01-create-feeder-tables.sql`
- Create: `src/FeederCopy.Core/Services/Data/Database.cs`, `Services/Logging/IErrorLog.cs`, `Services/Logging/FileErrorLog.cs`
- Create: `tests/FeederCopy.Tests/Support/TestFiles.cs`, `Support/TempDatabase.cs`
- Test: `tests/FeederCopy.Tests/Services/DatabaseTests.cs`

**Interfaces:**
- Produces: `Database(string filePath)`, `SqliteConnection Open()`, `void EnsureSchema()`, `string FilePath`. `IErrorLog.Error(string context, Exception exception)`. `TestFiles.TempPath(ext)`, `WriteSpreadsheetMl(rows)`, `WriteXlsx(rows)`, `WriteRawText(content, ext)`. `TempDatabase { Database Db; string FilePath }`.

- [ ] **Step 1: Scaffold.** Copy Jonnel's Core and Tests csproj files and adapt them:
  - Core: drop MySqlConnector, and set the embedded resource LogicalName to `FeederCopy.Schema.%(Filename)%(Extension)`.
  - Tests: as in Jonnel, referencing Core.

  Create a classic `.sln` with both projects. The App project is created in Task 12.
- [ ] **Step 2: Copy rules and ignore file.**
  - Copy Jonnel's `AGENTS.md`. Rename ConsumptionMonitor to FeederCopy, change the title to "Feeder kWh Copy Agent Rules", and change the read-only line to "The As-Billed files and feeder list files are read-only inputs. Never modify them."
  - `.gitignore`: `bin/ obj/ *.user .vs/ publish/ TestResults/ *.db *.db-* logs/ *.xls *.xlsx *.csv settings.json .claude/settings.local.json*`
- [ ] **Step 3: Schema script** `docs/sql/2026-10-20-01-create-feeder-tables.sql`:

```sql
CREATE TABLE Feeders (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE COLLATE NOCASE,
    CreatedAt TEXT NOT NULL
);
CREATE TABLE FeederConsumers (
    Id INTEGER PRIMARY KEY,
    FeederId INTEGER NOT NULL REFERENCES Feeders(Id) ON DELETE CASCADE,
    BusId TEXT NULL,
    ConsumerId TEXT NOT NULL UNIQUE,
    Position INTEGER NOT NULL
);
CREATE INDEX IX_FeederConsumers_Feeder_Position ON FeederConsumers(FeederId, Position);
```

- [ ] **Step 4: Write `DatabaseTests`.**
  - `EnsureSchema_NewFile_CreatesTablesAndSetsUserVersion1`: the tables `Feeders` and `FeederConsumers` exist, and `PRAGMA user_version` = 1.
  - `EnsureSchema_RunTwice_IsNoOp`.
  - `EnsureSchema_NewerVersion_Throws`: set user_version 5 and expect `InvalidOperationException` whose message contains "newer version".
  - `Open_EnablesForeignKeys`: `PRAGMA foreign_keys` = 1.
- [ ] **Step 5: Run** `dotnet test tests/FeederCopy.Tests`. Expected: the build fails because `Database` is missing.
- [ ] **Step 6: Implement.** Copy Jonnel's `Database.cs`, `IErrorLog.cs`, `FileErrorLog.cs`, `TestFiles.cs` and `TempDatabase.cs`, with these changes:
  - Prefix `FeederCopy.Schema.`.
  - Remove the legacy "version 0 with Imports table" check.
  - Add `PRAGMA foreign_keys = ON;` to `Open()`.
  - The newer-version message names `feeders.db`.
  - The `TempPath` prefix is `fc-test-`.
  - Drop LayoutA/LayoutB from TestFiles.
- [ ] **Step 7: Run** `dotnet test tests/FeederCopy.Tests`. Expected: PASS (4 tests).
- [ ] **Step 8: Commit.** Run `git init`, then `git add -A`, then `git commit -m "Scaffold Feeder kWh Copy solution, rules and database"`.

---

### Task 2: Settings models, validator and store

**Files:**
- Create: `src/FeederCopy.Core/Models/Settings/AppSettings.cs`, `SettingsValidator.cs`
- Create: `src/FeederCopy.Core/Services/Settings/ISettingsStore.cs`, `SettingsStore.cs`
- Test: `tests/FeederCopy.Tests/Models/SettingsValidatorTests.cs`, `tests/FeederCopy.Tests/Services/SettingsStoreTests.cs`

**Interfaces:**
- Produces:
  - `enum ExportStyle { OneWorkbook, SeparateFiles }`
  - `sealed record FeederColumns { required string BusId; required string ConsumerId; static FeederColumns Defaults }`, with defaults `"Bus ID"`, `"Consumer ID"`
  - `sealed record AsBilledColumns { required string AccountNumber; required string KwhUsed; required string BillMonth; static Defaults }`, with defaults `"ACCOUNTNUMBER"`, `"KWHUSED"`, `"BILLMONTH"`
  - `sealed record AppSettings { required FeederColumns Feeder; required AsBilledColumns AsBilled; required ExportStyle Export; static AppSettings Defaults }`, with Export default `OneWorkbook`
  - `sealed record SettingsLoadResult(AppSettings Settings, string? Problem)`
  - `static IReadOnlyList<string> SettingsValidator.Validate(AppSettings s)`
  - `ISettingsStore { SettingsLoadResult Load(); void Save(AppSettings s); }`, `SettingsStore(string path)`

- [ ] **Step 1: Write the tests.**
  - `Validate_Defaults_NoErrors`.
  - `Validate_BlankBusId_Error`: contains `"Enter the feeder list column name for Bus ID."`. Same pattern for Consumer ID and `"Enter the As-Billed column name for ACCOUNTNUMBER."`, `KWHUSED` and `BILLMONTH`.
  - `Validate_SameFeederNames_Error`: `"Bus ID and Consumer ID can't use the same column name."` (case-insensitive).
  - `Validate_DuplicateAsBilledNames_Error`: `"The As-Billed column names must all be different."`
  - `Load_MissingFile_DefaultsNoProblem`.
  - `Load_BadJson_DefaultsWithProblem`: Problem starts with `"settings.json could not be used ("`.
  - `Load_InvalidValues_DefaultsWithProblem`.
  - `SaveThenLoad_RoundTrips`, including `ExportStyle.SeparateFiles`.
- [ ] **Step 2: Run** `dotnet test tests/FeederCopy.Tests`. Expected: the build fails.
- [ ] **Step 3: Implement.** Copy Jonnel's `SettingsStore` unchanged apart from the types. The enum is serialized as a string (`JsonStringEnumConverter`).
- [ ] **Step 4: Run tests.** Expected: PASS.
- [ ] **Step 5: Commit** with the message `"Add settings models, validator and store"`.

---

### Task 3: Feeder layout rules (pure)

**Files:**
- Create: `src/FeederCopy.Core/Models/Feeders/FeederModels.cs`, `FeederLayout.cs`, `FeederRules.cs`, `FeederDiff.cs`, `FeederPreview.cs`
- Test: `tests/FeederCopy.Tests/Models/FeederLayoutTests.cs`, `FeederRulesTests.cs`, `FeederDiffTests.cs`

**Interfaces:**
- Produces:
  - `sealed record ConsumerEntry(string? BusId, string ConsumerId)`
  - `sealed record FeederSummary(long Id, string Name, int Consumers, int Buses)`
  - `sealed record ConsumerLocation(long FeederId, string FeederName, string? BusId)`
  - `sealed record ConsumerRow(int Count, string BusLabel, string ConsumerId)`
  - `static class BusLabels { const string Unknown = "Unknown"; static string For(string? busId) }`
  - `static class FeederLayout`:
    - `string? NormalizeBus(string? text)`: trim; empty becomes null
    - `string NormalizeConsumer(string text)`: trim
    - `List<ConsumerEntry> Arrange(IEnumerable<ConsumerEntry> entries)`
    - `List<ConsumerEntry> Add(IReadOnlyList<ConsumerEntry> entries, string? busId, string consumerId)`
    - `List<ConsumerEntry> Remove(IReadOnlyList<ConsumerEntry> entries, string consumerId)`
    - `List<ConsumerEntry> SetBus(IReadOnlyList<ConsumerEntry> entries, string consumerId, string? busId)`
    - `IReadOnlyList<string> Buses(IReadOnlyList<ConsumerEntry> entries)`: distinct non-null, in block order
    - `IReadOnlyList<ConsumerRow> ToRows(IReadOnlyList<ConsumerEntry> entries)`: Count 1..n
  - `static class FeederRules`:
    - `string? ValidateName(string? name, IEnumerable<FeederSummary> existing, long? exceptId)`
    - `string? ValidateConsumerId(string? id)`
    - `string AlreadyIn(string consumerId, ConsumerLocation where)`
  - `sealed record FeederDiffResult(int Added, int Removed, int BusChanged)` with `ToString()`, and `static FeederDiffResult FeederDiff.Compare(IReadOnlyList<ConsumerEntry> saved, IReadOnlyList<ConsumerEntry> incoming)`
  - `enum PreviewMode { NewFeeder, Update }`, `sealed record FeederPreview(PreviewMode Mode, string Name, string Summary, IReadOnlyList<string> Problems, bool CanSave)`

`Arrange` is the single ordering rule. Everything else is built on it:

```
group entries by BusId (OrdinalIgnoreCase), groups in order of first appearance,
entries keep their relative order inside a group,
each entry's BusId is rewritten to its group's first spelling,
the null-bus group (Unknown) is always last.
Add = Arrange(entries + [new(NormalizeBus(busId), NormalizeConsumer(consumerId))])
SetBus = Add(Remove(entries, consumerId), busId, consumerId)
```

- [ ] **Step 1: Write `FeederLayoutTests`.** Use buses `A`, `B` and IDs `c1…`.
  - `Arrange_GroupsInterleavedBusesByFirstAppearance`: `[A c1, B c2, A c3]` gives `[A c1, A c3, B c2]`.
  - `Arrange_UnknownAlwaysLast`: `[null c1, A c2]` gives `[A c2, null c1]`.
  - `Arrange_KeepsFirstSpelling`: `[CONSjunc14Se c1, consjunc14se c2]` gives both with `CONSjunc14Se`.
  - `Add_ExistingBus_InsertsAfterLastOfThatBus`: `[A c1, A c2, B c3]` plus `(A, c9)` gives `[A c1, A c2, A c9, B c3]`.
  - `Add_NewBus_GoesAboveUnknown`: `[A c1, null c2]` plus `(Z, c9)` gives `[A c1, Z c9, null c2]`.
  - `Add_BlankBus_GoesToUnknownAtBottom`: bus `"  "` gives the last entry with a null bus.
  - `Add_TrimsConsumerAndBus`: `(" A ", " c9 ")` gives `A c9`.
  - `SetBus_UnknownToExisting_MovesIntoBlock`.
  - `Remove_DropsOnlyThatConsumer`.
  - `ToRows_NumbersFromOne_UnknownLabel`: the row for the null bus has BusLabel `"Unknown"`, and Counts are 1, 2, 3.
  - `Buses_DistinctInBlockOrder`.
- [ ] **Step 2: Write `FeederRulesTests`.**
  - `ValidateName_Blank`: `"Enter a feeder name."`
  - `ValidateName_Duplicate_CaseInsensitive`: `"A feeder named 'new york feeder' already exists."`
  - `ValidateName_SameIdAllowed`: renaming to its own name gives null.
  - `ValidateConsumerId_Blank`: `"Enter a consumer ID."`
  - `AlreadyIn_WithBus`: `"Consumer c1 is already in New York Feeder under bus A."`
  - `AlreadyIn_Unknown`: `"Consumer c1 is already in New York Feeder under bus Unknown."`
- [ ] **Step 3: Write `FeederDiffTests`.**
  - `Compare_CountsAddedRemovedBusChanged`: saved `[A c1, A c2, B c3]`, incoming `[A c1, B c2, B c4]` gives (1, 1, 1).
  - `ToString`: `"1 added, 1 removed, 1 moved to a different bus"`.
  - `Compare_BusCaseOnlyChange_NotCounted`.
- [ ] **Step 4: Run the tests.** Expected: the build fails.
- [ ] **Step 5: Implement** the signatures above. Consumer IDs compare ordinally everywhere.
- [ ] **Step 6: Run the tests.** Expected: PASS.
- [ ] **Step 7: Commit** with the message `"Add feeder layout, rules and diff"`.

---

### Task 4: File readers and feeder list parser

**Files:**
- Create: `src/FeederCopy.Core/Services/Import/ITableReader.cs`, `TableReaderFactory.cs`, `SpreadsheetMlReader.cs`, `XlsxReader.cs`, `CsvFileReader.cs`, `HeaderIndex.cs`, `FeederListParser.cs`
- Create: `src/FeederCopy.Core/Models/Import/ImportModels.cs`
- Test: `tests/FeederCopy.Tests/Services/ReaderTests.cs`, `FeederListParserTests.cs`

**Interfaces:**
- Consumes: `FeederColumns` (Task 2), `ConsumerEntry`, `FeederLayout.Arrange/NormalizeBus/NormalizeConsumer` (Task 3).
- Produces:
  - `ITableReader { IEnumerable<IReadOnlyList<string?>> ReadRows(string path); }`
  - `static ITableReader TableReaderFactory.For(string path)`
  - `static IReadOnlyList<int> HeaderIndex.Require(IReadOnlyList<string?> header, IReadOnlyList<string> names)`: throws `InvalidDataException`
  - `sealed record RowError(int RowNumber, string Reason)`
  - `sealed record ParsedFeederList(string Path, IReadOnlyList<ConsumerEntry> Entries, IReadOnlyList<RowError> Warnings, string? FatalError)`
  - `static ParsedFeederList FeederListParser.Read(string path, FeederColumns columns)`

- [ ] **Step 1: Write `ReaderTests`.**
  - `SpreadsheetMl_HonoursSsIndex`.
  - `Xlsx_NumericCell_NoDecimalSuffix`: the cell value `1010001` (double) reads `"1010001"`.
  - `Xlsx_TextCell_KeepsLeadingZeros`: `"0101-0001"` stays as written.
  - `Csv_ReadsRows`.
  - `Factory_BinaryXls_ThrowsSaveAsHint`: the message contains `"Save As"`.
  - `Factory_UnknownFormat_Throws`.
- [ ] **Step 2: Write `FeederListParserTests`.** Use the header `Count, Bus ID, Consumer ID` unless the test says otherwise.
  - `Read_CountColumnIgnoredAndOptional`: a file with only `Consumer ID, Bus ID`, in that order, parses.
  - `Read_ArrangesIntoBusBlocks`: interleaved rows come out grouped, as in Task 3.
  - `Read_BlankRowsSkipped`.
  - `Read_BusWithoutConsumer_Warning`: Warnings contains `RowError(row, "Bus ID without a consumer ID; skipped")`.
  - `Read_ConsumerWithoutBus_ImportsAsUnknown`.
  - `Read_DuplicateConsumer_Fatal`: `"Consumer ID c1 appears more than once (rows 3, 7)."`
  - `Read_MissingColumn_Fatal`: contains `"Missing column(s): Bus ID"` and `"Settings tab"`.
  - `Read_Empty_Fatal`: `"The file is empty."`
  - `Read_NoConsumers_Fatal`: `"No consumers were found in the file."`
  - `Read_LockedFile_FatalCloseInExcel`: open the file with `FileShare.None` while reading. FatalError contains `"close it and try again"`.
- [ ] **Step 3: Run the tests.** Expected: the build fails.
- [ ] **Step 4: Implement.**
  - Copy Jonnel's three readers and the factory, renaming them to `ITableReader` and `TableReaderFactory`. The unsupported-format message becomes `"Unsupported file format. Expected an .xls XML spreadsheet, an .xlsx workbook, or a .csv file."`
  - `HeaderIndex.Require` finds names case-insensitively after trimming. The missing message is `"Missing column(s): {names}. This file's columns: {found}. Change the column names in the Settings tab."`
  - The header row is the first non-blank row.
  - `FeederListParser` catches the exception types the same way as Jonnel's `BillingFileParser`, with the same messages.
- [ ] **Step 5: Run the tests.** Expected: PASS.
- [ ] **Step 6: Commit** with the message `"Add table readers and feeder list parser"`.

---

### Task 5: As-Billed reader

**Files:**
- Create: `src/FeederCopy.Core/Models/Billing/BillMonth.cs` (copied from Jonnel, namespace changed)
- Create: `src/FeederCopy.Core/Models/Copy/KwhSnapshot.cs`
- Create: `src/FeederCopy.Core/Services/Import/IAsBilledReader.cs`, `AsBilledReader.cs`
- Test: `tests/FeederCopy.Tests/Services/AsBilledReaderTests.cs`, `tests/FeederCopy.Tests/Models/BillMonthTests.cs` (copied)

**Interfaces:**
- Consumes: `TableReaderFactory`, `HeaderIndex`, `RowError` (Task 4), `AsBilledColumns` (Task 2).
- Produces:
  - `sealed record AsBilledEntry(int RowNumber, double? Kwh)`, where null means the kWh text could not be parsed
  - `sealed record KwhSnapshot(string FileName, BillMonth? Month, IReadOnlyDictionary<string, IReadOnlyList<AsBilledEntry>> ByAccount, int RowsRead, IReadOnlyList<RowError> Errors, string? FatalError)`: keys trimmed, ordinal
  - `IAsBilledReader { KwhSnapshot Read(string path, AsBilledColumns columns, IProgress<int>? progress); }`, implemented by `AsBilledReader`

This is the seam for the future server reader: the copy step only ever sees `KwhSnapshot`.

- [ ] **Step 1: Write the tests.** Use the header `ADDRESS, ACCOUNTNUMBER, ACCOUNTNAME, RATECODE, KWHUSED, BILLMONTH` with fictional rows.
  - `Read_IndexesByTrimmedAccount`.
  - `Read_DuplicateAccount_KeepsBothEntries`: 2 entries, with row numbers.
  - `Read_UnreadableKwh_EntryWithNullKwh`.
  - `Read_CommasInKwh_Parsed`: `"1,234.5"` gives 1234.5.
  - `Read_MissingAccount_RowError`: `"Missing account number"`.
  - `Read_TwoMonths_Fatal`: contains `"more than one bill month"`.
  - `Read_MonthFormats`: `202607`, `072026` and `0726` all give Jul 2026.
  - `Read_MissingColumn_Fatal`.
  - `Read_LockedFile_FatalCloseInExcel`.
  - `Read_ReportsProgressEvery5000Rows`.
- [ ] **Step 2: Run the tests.** Expected: the build fails.
- [ ] **Step 3: Implement.**
  - kWh parsing is `TryParseKwh` from Jonnel's `BillingRowParser` (strip commas, invariant culture).
  - Rows with an invalid month are row errors: `"Invalid bill month '{x}' (expected YYYYMM, MMYYYY or MMYY)"`.
  - `Month` is the single distinct month across the valid rows.
  - No valid rows gives `"No valid rows were found in the file."`
- [ ] **Step 4: Run the tests.** Expected: PASS.
- [ ] **Step 5: Commit** with the message `"Add As-Billed reader"`.

---

### Task 6: kWh copy and problem report (pure)

**Files:**
- Create: `src/FeederCopy.Core/Models/Copy/CopyModels.cs`, `KwhCopier.cs`, `CopyReport.cs`
- Test: `tests/FeederCopy.Tests/Models/KwhCopierTests.cs`, `CopyReportTests.cs`

**Interfaces:**
- Consumes: `ConsumerEntry`, `BusLabels` (Task 3), `KwhSnapshot`, `BillMonth` (Task 5).
- Produces:
  - `sealed record FeederSource(string Name, IReadOnlyList<ConsumerEntry> Consumers)`
  - `sealed record FilledRow(int Count, string BusLabel, string ConsumerId, double? Kwh, string? Problem)`
  - `sealed record FilledFeeder(string Name, IReadOnlyList<FilledRow> Rows)`
  - `sealed record FeederTotals(string Name, int Consumers, int Copied, int NotFound, int Duplicates, int Unreadable, double TotalKwh)`
  - `sealed record CopyProblem(string Feeder, int Count, string BusLabel, string ConsumerId, string Problem)`
  - `sealed record CopyResult(BillMonth Month, string SourceFile, IReadOnlyList<FilledFeeder> Feeders, IReadOnlyList<FeederTotals> Totals, IReadOnlyList<CopyProblem> Problems)`
  - `static CopyResult KwhCopier.Fill(IReadOnlyList<FeederSource> feeders, KwhSnapshot snapshot, BillMonth month)`
  - `static string CopyReport.ToText(CopyResult result)`

- [ ] **Step 1: Write `KwhCopierTests`.**
  - `Fill_FoundOnce_CopiesValue`.
  - `Fill_ZeroAndNegative_CopiedAsIs`: 0 and -5 are copied, with no Problem.
  - `Fill_TrimmedMatch`: the feeder ID `" c1 "` matches the snapshot key `"c1"`.
  - `Fill_NotFound`: Kwh null, Problem `"Not in As-Billed"`.
  - `Fill_Duplicate`: Kwh null, Problem `"Appears 2 times in As-Billed"`.
  - `Fill_Unreadable`: Kwh null, Problem `"kWh could not be read in As-Billed (row 15)"`.
  - `Fill_KeepsOrderCountAndUnknownLabel`.
  - `Fill_Totals`: Consumers/Copied/NotFound/Duplicates/Unreadable/TotalKwh per feeder. TotalKwh sums only the copied values.
  - `Fill_ProblemsListedInFeederThenRowOrder`.
- [ ] **Step 2: Write `CopyReportTests`.**
  - `ToText_HeaderAndLines`: the first line is `"kWh copy for Jul 2026 from as billed 07-2026.xls"`, then one totals line per feeder, `"New York Feeder: 120 consumers, 117 copied, 2 not found, 1 duplicate, 0 unreadable, 15,230.50 kWh"`, then the problem lines, `"New York Feeder · #12 · bus A · c1 · Not in As-Billed"`.
  - `ToText_NoProblems`: ends with `"No problems."`
- [ ] **Step 3: Run the tests.** Expected: the build fails.
- [ ] **Step 4: Implement.** Numbers in the report use `N2` with invariant culture.
- [ ] **Step 5: Run the tests.** Expected: PASS.
- [ ] **Step 6: Commit** with the message `"Add kWh copy and problem report"`.

---

### Task 7: Excel export

**Files:**
- Create: `src/FeederCopy.Core/Models/Copy/ExportNames.cs`
- Create: `src/FeederCopy.Core/Services/Export/IFeederExporter.cs`, `FeederExporter.cs`
- Test: `tests/FeederCopy.Tests/Models/ExportNamesTests.cs`, `tests/FeederCopy.Tests/Services/FeederExporterTests.cs`

**Interfaces:**
- Consumes: `CopyResult`, `FilledFeeder` (Task 6).
- Produces:
  - `static class ExportNames`:
    - `IReadOnlyList<string> SheetNames(IReadOnlyList<string> feederNames)`
    - `string FeederFileName(string feederName, BillMonth month)`: `"New York Feeder - Jul 2026.xlsx"`
    - `string WorkbookFileName(BillMonth month)`: `"Feeders - Jul 2026.xlsx"`
  - `IFeederExporter`:
    - `void WriteWorkbook(string path, CopyResult result)`
    - `IReadOnlyList<string> SeparatePaths(string folder, CopyResult result)`
    - `void WriteSeparate(string folder, CopyResult result)`

- [ ] **Step 1: Write `ExportNamesTests`.**
  - `SheetNames_ReplacesInvalidChars`: `"A/B:C?"` gives `"A-B-C-"`.
  - `SheetNames_CutsTo31`.
  - `SheetNames_MakesUnique`: `["X/Y", "X:Y"]` gives `["X-Y", "X-Y (2)"]`, still 31 characters or fewer.
  - `SheetNames_BlankBecomesFeeder`.
  - `SheetNames_StripsLeadingTrailingApostrophe`.
  - `FeederFileName_RemovesInvalidFileChars`.
- [ ] **Step 2: Write `FeederExporterTests`.** These read the output back with ClosedXML.
  - `Workbook_OneSheetPerFeeder_InFeederOrder`.
  - `Sheet_Layout`:
    - A1 holds the feeder name, A1:D1 is merged and A1 is bold.
    - Row 2 reads `Count | Bus ID | Consumer ID | kWh (Jul 2026)`.
    - Data starts at row 3.
    - Consumer ID cells have text type, and `"0101"` stays `"0101"`.
    - kWh cells are numbers.
    - A problem row's kWh cell is blank, with the light fill `XLColor.FromArgb(255, 242, 204)`.
    - The sheet is frozen at 2 rows.
  - `Separate_OneFilePerFeeder_NamedByFeederAndMonth`.
  - `SeparatePaths_MatchesWrittenFiles`.
- [ ] **Step 3: Run the tests.** Expected: the build fails.
- [ ] **Step 4: Implement** with ClosedXML. Columns are auto-fitted on all rows. The invalid sheet characters are `: \ / ? * [ ]`, replaced with `-`.
- [ ] **Step 5: Run the tests.** Expected: PASS.
- [ ] **Step 6: Commit** with the message `"Add Excel feeder export"`.

---

### Task 8: Feeder service (SQLite)

**Files:**
- Create: `src/FeederCopy.Core/Services/Data/IFeederService.cs`, `FeederService.cs`
- Test: `tests/FeederCopy.Tests/Services/FeederServiceTests.cs`

**Interfaces:**
- Consumes: `Database` (Task 1), `ConsumerEntry`, `FeederSummary`, `ConsumerLocation` (Task 3).
- Produces `IFeederService`:
  - `IReadOnlyList<FeederSummary> GetFeeders()`: ordered by Name
  - `IReadOnlyList<ConsumerEntry> GetConsumers(long feederId)`: ordered by Position
  - `ConsumerLocation? FindConsumer(string consumerId)`
  - `IReadOnlyList<(string ConsumerId, ConsumerLocation Where)> FindConsumers(IEnumerable<string> consumerIds)`
  - `long CreateFeeder(string name, IReadOnlyList<ConsumerEntry> consumers)`
  - `void SaveConsumers(IReadOnlyDictionary<long, IReadOnlyList<ConsumerEntry>> byFeeder)`: per feeder, delete its rows, then insert with `Position` = index + 1, all in one transaction
  - `void RenameFeeder(long feederId, string name)`
  - `void DeleteFeeder(long feederId)`

- [ ] **Step 1: Write the tests** on `TempDatabase`.
  - `CreateFeeder_ThenGetConsumers_KeepsOrderAndNullBus`.
  - `GetFeeders_CountsConsumersAndDistinctBuses`: a null bus is not counted as a bus.
  - `FindConsumer_ReturnsFeederAndBus`.
  - `FindConsumers_ManyIds`: works with more than 999 IDs (chunked).
  - `SaveConsumers_MoveBetweenFeeders_OneTransaction`: c1 moves from feeder 1 to feeder 2 with no unique-constraint error.
  - `SaveConsumers_RollsBackOnFailure`.
  - `DeleteFeeder_RemovesConsumers`.
  - `RenameFeeder_Persists`.
  - `CreatedAt_Format`: `"yyyy-MM-dd HH:mm:ss"`.
- [ ] **Step 2: Run the tests.** Expected: the build fails.
- [ ] **Step 3: Implement** with Dapper and parameters only. In `SaveConsumers`, first delete every affected feeder's rows, then insert, so a moved ID never collides.
- [ ] **Step 4: Run the tests.** Expected: PASS.
- [ ] **Step 5: Commit** with the message `"Add feeder service"`.

---

### Task 9: Settings presenter

**Files:**
- Create: `src/FeederCopy.Core/Presenters/Settings/ISettingsView.cs`, `SettingsPresenter.cs`
- Create: `tests/FeederCopy.Tests/Support/Fakes.cs`: `FakeErrorLog`, `FakeSettingsStore`, `FakeSettingsView`
- Test: `tests/FeederCopy.Tests/Presenters/SettingsPresenterTests.cs`

**Interfaces:**
- Consumes: `AppSettings`, `SettingsValidator`, `ISettingsStore` (Task 2).
- Produces:
  - `ISettingsView`:
    - `void ShowSettings(AppSettings s)`
    - `AppSettings ReadSettings()`: carries every field
    - `void ShowValidation(IReadOnlyList<string> errors)`
    - `void ShowSaved()`
  - `SettingsPresenter(ISettingsView view, ISettingsStore store)`:
    - `AppSettings Current`
    - `event Action? SettingsChanged`
    - `string? Load()`: returns the load problem
    - `void OnSaveClicked()`
    - `void OnRevertClicked()`
    - `void OnDefaultsClicked()`
    - `void RememberExportStyle(ExportStyle style)`

- [ ] **Step 1: Write the tests.**
  - `Load_ShowsSettingsAndReturnsProblem`.
  - `Save_Invalid_ShowsErrorsDoesNotSave`.
  - `Save_Valid_SavesRaisesChanged`.
  - `Revert_ShowsCurrentWithoutSaving`.
  - `Defaults_ShowsDefaultsWithoutSaving`.
  - `RememberExportStyle_SavesOnlyWhenDifferent_UpdatesView`.
- [ ] **Step 2: Run, implement, run.** Expected: FAIL, then PASS.
- [ ] **Step 3: Commit** with the message `"Add settings presenter"`.

---

### Task 10: Feeders presenter

**Files:**
- Create: `src/FeederCopy.Core/Presenters/Feeders/IFeedersView.cs`, `FeedersPresenter.cs`
- Modify: `tests/FeederCopy.Tests/Support/Fakes.cs` (add `FakeFeedersView`)
- Test: `tests/FeederCopy.Tests/Presenters/FeedersPresenterTests.cs`, with the real `FeederService` on `TempDatabase` and files from `TestFiles`

**Interfaces:**
- Consumes:
  - `IFeederService` (Task 8)
  - `FeederListParser` (Task 4)
  - `FeederLayout`, `FeederRules`, `FeederDiff`, `FeederPreview` (Task 3)
  - `AppSettings` (Task 2)
  - `IErrorLog`
- Produces `IFeedersView`:
  - `void ShowFeeders(IReadOnlyList<FeederSummary> feeders, long? selectedId)`
  - `void ShowNoFeeders()`
  - `void ShowConsumers(FeederSummary feeder, IReadOnlyList<ConsumerRow> rows, IReadOnlyList<string> buses, IReadOnlyList<FeederSummary> otherFeeders)`
  - `void ShowPreview(FeederPreview preview)`
  - `void HidePreview()`
  - `void SetBusy(bool busy, string? message)`
  - `void ClearAddForm()`
  - `void ShowAddError(string? message)`
  - `void SelectConsumer(string consumerId)`
  - `void ShowSearchResult(string? message)`
  - `string? PickOpenFile(string title, string filter)`
  - `bool Confirm(string title, string message)`
  - `void ShowError(string message)`
- Produces `FeedersPresenter(IFeedersView view, IFeederService feeders, Func<AppSettings> settings, IErrorLog log)`:
  - `event Action? FeedersChanged`
  - `void Load()`
  - `void OnFeederSelected(long feederId)`
  - `Task OnImportNewClickedAsync()`
  - `Task OnUpdateClickedAsync()`
  - `void OnPreviewNameChanged(string name)`
  - `void OnSavePreviewClicked()`
  - `void OnCancelPreviewClicked()`
  - `void OnRenameClicked(string name)`
  - `void OnDeleteClicked()`
  - `void OnAddClicked(string? busId, string? consumerId)`
  - `void OnSetBusClicked(string consumerId, string? busId)`
  - `void OnMoveClicked(string consumerId, long targetFeederId)`
  - `void OnRemoveClicked(string consumerId)`
  - `void OnSearch(string? text)`

Behavior the tests pin:
- **Import new:** pick a file, parse it in `Task.Run`, then show a NewFeeder preview.
  - The name is pre-filled from the file name without its extension.
  - Summary: `"412 consumers · 38 buses · 3 rows skipped"`.
  - Problems: the parser's FatalError, then each warning as `"Row 7: …"`, then `FeederRules.AlreadyIn` for every ID already in another feeder (via `FindConsumers`).
  - `CanSave` is false when there is a FatalError or any AlreadyIn problem, or when the name is invalid.
- **Update:** the same, for the selected feeder. Summary: `"{FeederDiff} · 412 consumers"`. AlreadyIn ignores IDs that are in the selected feeder itself. Save replaces the list through `SaveConsumers`.
- **Every successful change** (save, rename, delete, add, set bus, move, remove) reloads the list, keeps the selection, and raises `FeedersChanged`.
- **Add:** check `ValidateConsumerId`, then `FindConsumer`. If found, call `ShowAddError(AlreadyIn(...))`. Otherwise call `FeederLayout.Add`, save, `ClearAddForm`, then `SelectConsumer(id)`.
- **Delete and Remove** ask Confirm first:
  - `"Delete feeder 'X' and its N consumers?"`
  - `"Remove consumer c1 from X?"`
  On No, nothing changes.
- **Move:** remove the consumer from the source and `FeederLayout.Add` it to the target, keeping its bus, in one `SaveConsumers` call.
- **Search:** trimmed exact consumer ID. If found, select that feeder, `SelectConsumer`, and `ShowSearchResult(null)`. If not, `"Consumer c1 isn't in any feeder."`
- **Service exceptions** are logged with context, plus `ShowError(short message)`.

- [ ] **Step 1: Write the tests** (one per bullet above, at least):
  - `Load_NoFeeders_ShowsEmpty`
  - `ImportNew_ShowsPreviewWithSummary`
  - `ImportNew_ConsumerInOtherFeeder_CannotSave`
  - `ImportNew_Save_CreatesAndSelects_RaisesChanged`
  - `ImportNew_DuplicateName_CannotSave`
  - `Update_ShowsDiffSummary`
  - `Update_Save_ReplacesList`
  - `Add_ExistingBus_InsertedInBlock`
  - `Add_AlreadyInFeeder_ShowsAddError`
  - `Add_BlankBus_UnknownAtBottom`
  - `SetBus_MovesIntoBlock`
  - `Move_ToOtherFeeder`
  - `Remove_ConfirmNo_NoChange`
  - `Delete_ConfirmYes_Removed`
  - `Rename_Invalid_ShowsError`
  - `Search_NotFound_Message`
  - `ServiceThrows_LoggedAndShown`
- [ ] **Step 2: Run, implement, run.** Expected: FAIL, then PASS. If the presenter passes 400 lines, move the preview building into `Core/Presenters/Feeders/FeederPreviewBuilder.cs`.
- [ ] **Step 3: Commit** with the message `"Add feeders presenter"`.

---

### Task 11: Copy presenter

**Files:**
- Create: `src/FeederCopy.Core/Presenters/Copy/ICopyView.cs`, `CopyPresenter.cs`
- Modify: `tests/FeederCopy.Tests/Support/Fakes.cs` (add `FakeCopyView`)
- Test: `tests/FeederCopy.Tests/Presenters/CopyPresenterTests.cs`, with the real `FeederService`, `AsBilledReader` and `FeederExporter` on temporary files

**Interfaces:**
- Consumes:
  - `IFeederService` (Task 8)
  - `IAsBilledReader`, `KwhSnapshot` (Task 5)
  - `KwhCopier`, `CopyReport` (Task 6)
  - `IFeederExporter`, `ExportNames` (Task 7)
  - `AppSettings`, `ExportStyle` (Task 2)
- Produces `ICopyView`:
  - `void ShowFileRead(string fileName, BillMonth? fileMonth, int rows, int skipped)`
  - `void SetMonth(int? month, int? year)`
  - `void ShowResult(IReadOnlyList<FeederTotals> totals, IReadOnlyList<CopyProblem> problems, string heading)`
  - `void ClearResult(string message)`
  - `void SetExportStyle(ExportStyle style)`
  - `void SetBusy(bool busy, string? message)`
  - `void SetCopyEnabled(bool enabled)`
  - `void SetExportEnabled(bool enabled)`
  - `string? PickOpenFile(string title, string filter)`
  - `string? PickSaveFile(string filter, string defaultName)`
  - `string? PickFolder()`
  - `bool Confirm(string title, string message)`
  - `void ShowError(string message)`
  - `void ShowInfo(string message)`
- Produces `CopyPresenter(ICopyView view, IFeederService feeders, IAsBilledReader reader, IFeederExporter exporter, Func<AppSettings> settings, Action<ExportStyle> rememberStyle, IErrorLog log)`:
  - `void Load()`
  - `Task OnChooseFileClickedAsync()`
  - `Task OnCopyClickedAsync(int? month, int? year)`
  - `void OnFeedersChanged()`
  - `void OnSaveProblemsClicked()`
  - `void OnExportClicked(ExportStyle style)`

Behavior the tests pin:
- **Load:**
  - With no feeders: `ClearResult("Import a feeder list in the Feeders tab first.")` and Copy disabled.
  - Otherwise: `ClearResult("Choose an As-Billed file to start.")`.
  - `SetExportStyle(settings().Export)`, and Export disabled.
- **Choose file:**
  - Read in `Task.Run`, with progress text `"Reading… 25,000 rows"`.
  - FatalError gives `ShowError` and no snapshot.
  - Otherwise `ShowFileRead`, `SetMonth` from the file's month, and Copy enabled.
- **Copy:**
  - A missing month or year gives `ShowError("Choose the month and year.")`.
  - A chosen month different from the file's month asks `Confirm("Different month", "You chose Jun 2026, but the file contains Jul 2026. Use Jun 2026?")`. On No, it stops.
  - Then `KwhCopier.Fill` over every feeder, `ShowResult` with heading `"Jul 2026 · 3 feeders · 2 problems"`, and Export enabled.
- **OnFeedersChanged:** if a result exists, clear it with `"Feeders changed. Click Copy kWh again."` and disable Export. The snapshot is kept.
- **Save problems:** `PickSaveFile("Text file|*.txt", "kWh copy problems - Jul 2026.txt")`, then write `CopyReport.ToText`.
- **Export:**
  - `rememberStyle(style)` first.
  - OneWorkbook: `PickSaveFile("Excel workbook|*.xlsx", ExportNames.WorkbookFileName(month))`, then `WriteWorkbook`.
  - SeparateFiles: `PickFolder()`. If any `SeparatePaths` already exist, ask `Confirm("Replace files", "3 files already exist in this folder and will be replaced. Continue?")`. On No, nothing is written. Otherwise `WriteSeparate`.
  - Success: `ShowInfo("Exported 3 feeders.")`.
  - IO errors are logged, then `ShowError("Could not save the file. If it is open in Excel, close it and try again.")`.

- [ ] **Step 1: Write the tests:**
  - `Load_NoFeeders_Message`
  - `ChooseFile_Fatal_ShowsError`
  - `ChooseFile_PrefillsMonth_EnablesCopy`
  - `Copy_NoMonth_Error`
  - `Copy_MonthMismatch_ConfirmNo_Stops`
  - `Copy_ShowsTotalsProblemsHeading_EnablesExport`
  - `FeedersChanged_ClearsResult`
  - `SaveProblems_WritesReport`
  - `Export_Workbook_WritesFile_RemembersStyle`
  - `Export_Separate_ExistingFiles_ConfirmNo_WritesNothing`
  - `Export_Separate_WritesAll`
  - `Export_FileLocked_ShowsCloseInExcel`
- [ ] **Step 2: Run, implement, run.** Expected: FAIL, then PASS.
- [ ] **Step 3: Commit** with the message `"Add copy presenter"`.

---

### Task 12: App shell, theme and Settings view

**Files:**
- Create: `src/FeederCopy.App/FeederCopy.App.csproj`. Copy Jonnel's App csproj without DockPanelSuite, ScottPlot, Velopack or the icon resources. Set `<Version>1.0.0</Version>`, `<AssemblyName>FeederCopy</AssemblyName>` and `<Product>Feeder kWh Copy</Product>`, with SubType entries for every form and view. Add it to `FeederCopy.sln`.
- Create: `src/FeederCopy.App/Program.cs`, `AppServices.cs`, `Utils/AppTheme.cs`, `Utils/Dialogs.cs`
- Create: `src/FeederCopy.App/Forms/Shell/MainForm.cs`, `MainForm.Designer.cs`
- Create: `src/FeederCopy.App/Forms/Settings/SettingsView.cs`, `SettingsView.Designer.cs`

**Interfaces:**
- Consumes: every Core service and presenter above.
- Produces:
  - `AppServices(IFeederService Feeders, IAsBilledReader AsBilled, IFeederExporter Exporter, ISettingsStore Settings, IErrorLog Log)`
  - `AppTheme` with the `Style*` helpers that Tasks 13 and 14 call:
    - `StylePrimaryButton`, `StyleOutlineButton`, `StyleDangerButton`
    - `StyleComboBox`, `StyleTextBox`, `StyleRadio`
    - `StyleFieldLabel`, `StyleMutedLabel`, `StyleSectionLabel`
    - `StyleCard`, `StyleToolbar`, `StyleStatusStrip`, `StyleWarningBanner`
    - `StyleTabControl`, `StyleGrid`, `StyleListBox`, `StyleSummaryBox`
    - `AlignRight`
  - `MainForm` hosts the tabs **Feeders**, **Copy kWh** and **Settings**, a warning banner and a status strip.

- [ ] **Step 1: AppTheme and Dialogs.**
  - Copy the light-theme colors, fonts and the helpers listed above from Jonnel's `Utils/AppTheme.cs`. Leave out dark mode, charts, the rate palette and anything unused. Keep it under 400 lines.
  - Copy `Dialogs` with `AppName = "Feeder kWh Copy"` and add `PickFolder(owner)` using `FolderBrowserDialog`.
- [ ] **Step 2: Program and AppServices.**
  - Same shape as Jonnel's `Program.cs`: log, global handlers, then `new Database(Path.Combine(AppContext.BaseDirectory, "feeders.db")).EnsureSchema()`.
  - Build the services, then `Application.Run(new MainForm(services))`.
  - Startup failure message: `"…Install it in a folder you can write to, for example C:\FeederCopy."`
- [ ] **Step 3: SettingsView.**
  - Implements `ISettingsView`. Two cards: "Feeder list columns" (Bus ID, Consumer ID) and "As-Billed columns" (ACCOUNTNUMBER, KWHUSED, BILLMONTH).
  - An Export style combo with the options "One workbook, sheet per feeder" and "Separate file per feeder".
  - Buttons Save, "Undo unsaved changes" and "Fill in defaults", plus `lblValidation`.
  - The layout is in the Designer and uses `TableLayoutPanel`.
- [ ] **Step 4: MainForm.**
  - The constructor creates `SettingsPresenter`, then `FeedersPresenter` and `CopyPresenter` with `() => settingsPresenter.Current`.
  - `rememberStyle` is wired to `settingsPresenter.RememberExportStyle`, and `feedersPresenter.FeedersChanged` to `copyPresenter.OnFeedersChanged`.
  - It calls each view's `Attach`.
  - On `Shown`: `settingsPresenter.Load()`. A returned problem shows in the warning banner. Then `feedersPresenter.Load()` and `copyPresenter.Load()`.
  - The title is `"Feeder kWh Copy 1.0.0"`, read from the assembly's informational version.
  - Until Tasks 13 and 14, the Feeders and Copy kWh tabs host empty panels so the app builds and runs. Tasks 13 and 14 replace them.
- [ ] **Step 5: Build** with `dotnet build FeederCopy.sln`. Expected: 0 errors.
- [ ] **Step 6: Smoke start.**
  - `dotnet build src/FeederCopy.App -o "<scratchpad>/appbuild"`.
  - `Start-Process "<scratchpad>/appbuild/FeederCopy.exe" -PassThru`, then wait 8 s.
  - Expected: the process is still running, `<scratchpad>/appbuild/logs` doesn't exist, and `feeders.db` was created. Then `Stop-Process`.
- [ ] **Step 7: Commit** with the message `"Add app shell, theme and settings view"`.

---

### Task 13: Feeders view

**Files:**
- Create: `src/FeederCopy.App/Forms/Feeders/FeedersView.cs`, `FeedersView.Designer.cs`
- Modify: `MainForm.Designer.cs` (host the view in the Feeders tab), csproj SubType

**Interfaces:**
- Consumes: `IFeedersView`, `FeedersPresenter` (Task 10).

Layout (Designer, `TableLayoutPanel`):

```
┌ Feeders ──────────────┐┌ New York Feeder ── [name____][Rename] [Delete] ────────────┐
│ [Import feeder list…] ││ Bus ID [combo, editable ▾]  Consumer ID [______]  [Add]     │
│ [Update from list…]   ││ lblAddError                                                 │
│ lstFeeders            ││ Search [______]  lblSearch                                  │
│  New York Feeder 412  ││ dgvConsumers: Count | Bus ID | Consumer ID                  │
│  Washington …         ││ Selected: [bus combo▾][Set bus ID] [feeder▾][Move] [Remove] │
└───────────────────────┘└─────────────────────────────────────────────────────────────┘
pnlPreview (docked top over the right side when visible):
  Name [__________]  Summary label  Problems list  [Save feeder | Replace list] [Cancel]
```

- [ ] **Step 1: Build the Designer layout and code-behind.**
  - Handlers relay to the presenter.
  - `dgvConsumers` is VirtualMode, read-only and full-row select.
  - The Unknown rows' Bus ID cell uses `AppTheme` muted text.
  - Search fires on Enter.
  - Busy state disables everything and shows the message in the status label.
  - Empty state: `"No feeders yet. Click Import feeder list… to add one."`
- [ ] **Step 2: Build and smoke start** as in Task 12, Step 6.
- [ ] **Step 3: Click-only check.** In a scratch console that references `<scratch>/appbuild` (see Jonnel HANDOFF §5), create `FeedersView`, call `ShowConsumers`, `ShowPreview` and `ShowNoFeeders` with sample data, `Application.DoEvents()`, then dispose. Expected: no exception.
- [ ] **Step 4: Commit** with the message `"Add feeders view"`.

---

### Task 14: Copy kWh view

**Files:**
- Create: `src/FeederCopy.App/Forms/Copy/CopyView.cs`, `CopyView.Designer.cs`
- Modify: `MainForm.Designer.cs` (host the view in the Copy kWh tab), csproj SubType

**Interfaces:**
- Consumes: `ICopyView`, `CopyPresenter` (Task 11).

Layout:

```
[Choose As-Billed file…]  lblFile ("as billed 07-2026.xls · Jul 2026 · 113,820 rows · 3 skipped")
Month [Jul ▾]  Year [2026]  [Copy kWh]
lblHeading
dgvTotals: Feeder | Consumers | Copied | Not found | Duplicates | Unreadable | kWh total
dgvProblems: Feeder | # | Bus ID | Consumer ID | Problem          [Save problem list…]
Export as (•) One workbook, sheet per feeder  ( ) Separate file per feeder   [Export to Excel…]
```

- [ ] **Step 1: Build the Designer layout and code-behind.**
  - The month combo lists January to December. Year is a numeric input from 1900 to 2100.
  - Number columns are right-aligned, with kWh formatted `N2`.
- [ ] **Step 2: Build, smoke start, and run the click-only check** (`ShowResult` with sample totals and problems).
- [ ] **Step 3: Commit** with the message `"Add copy kWh view"`.

---

### Task 15: Final verification and publish

- [ ] **Step 1:** `dotnet build FeederCopy.sln`. Expected: 0 errors, 0 warnings from our code.
- [ ] **Step 2:** `dotnet test tests/FeederCopy.Tests`. Expected: all pass. Report the count.
- [ ] **Step 3: Check the rules.**
  - No `System.Windows.Forms` usage under `src/FeederCopy.Core` (Grep `System.Windows.Forms`).
  - No file over 1,000 lines.
  - No `//` or `/*` comments under `src/` (Grep).
- [ ] **Step 4: End-to-end on fictional data.**
  - In the scratchpad, write a 2-feeder list (`.xlsx`) and an As-Billed SpreadsheetML file with one duplicate and one missing account.
  - Use the scratch console to drive the real presenters with real services on a scratch `feeders.db`: import, add a consumer, copy, then export both styles.
  - Open the outputs with ClosedXML and check the layout from Task 7.
- [ ] **Step 5:** `dotnet publish src/FeederCopy.App -c Release -r win-x64 --self-contained true -o publish/FeederCopy`.
- [ ] **Step 6: Commit** with the message `"Feeder kWh Copy 1.0.0"`. Tell the user: open `FeederCopy.sln` in Visual Studio, build (Ctrl+Shift+B), run, and check the screens. The visual review is theirs.
