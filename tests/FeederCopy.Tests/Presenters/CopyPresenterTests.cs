using ClosedXML.Excel;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Presenters.Copy;
using FeederCopy.Core.Services.Data;
using FeederCopy.Core.Services.Export;
using FeederCopy.Core.Services.Import;

namespace FeederCopy.Tests.Presenters
{
    public sealed class CopyPresenterTests : IDisposable
    {
        private static readonly string?[] Header = ["ACCOUNTNUMBER", "KWHUSED", "BILLMONTH"];

        private readonly TempDatabase _temp = new();
        private readonly FeederService _service;
        private readonly FakeCopyView _view = new();
        private readonly FakeErrorLog _log = new();
        private readonly List<ExportStyle> _remembered = [];
        private readonly ReadingService _readings;
        private readonly CopyPresenter _presenter;
        private int _readingsSaved;

        public CopyPresenterTests()
        {
            _service = new FeederService(_temp.Db);
            _readings = new ReadingService(_temp.Db);
            _presenter = new CopyPresenter(_view, _service, _readings, new AsBilledReader(), new FeederExporter(), () => AppSettings.Defaults, _remembered.Add, _log);
            _presenter.ReadingsSaved += () => _readingsSaved++;
        }

        [Fact]
        public async Task Copy_SavesReadingsForTheMonth_RaisesEvent()
        {
            await ReadyAsync();
            await _presenter.OnCopyClickedAsync(7, 2026);
            var year = _readings.GetYear(2026).OrderBy(r => r.ConsumerId).ToList();
            Assert.Equal(
                [new FeederCopy.Core.Models.Copy.ReadingRecord("c1", "202607", 10, "Active"),
                 new FeederCopy.Core.Models.Copy.ReadingRecord("c2", "202607", null, "Inactive"),
                 new FeederCopy.Core.Models.Copy.ReadingRecord("c3", "202607", null, "Duplicate in As-Billed")],
                year);
            Assert.Equal(1, _readingsSaved);
        }

        [Fact]
        public async Task Copy_NewestMonth_SetsCurrentStatusFromBills()
        {
            int updates = 0;
            _presenter.StatusesUpdated += () => updates++;
            _service.CreateFeeder("F", [E("A", "c1"), E("A", "c2")]);
            _service.SetActive("c1", false);
            _presenter.Load();
            _view.NextOpenFile = AsBilled(["c1", "10", "202607"], ["c9", "4", "202607"]);
            await _presenter.OnChooseFileClickedAsync();
            await _presenter.OnCopyClickedAsync(7, 2026);
            Assert.Equal([("c1", true), ("c2", false)], _service.GetAllConsumers().Select(c => (c.Entry.ConsumerId, c.Entry.Active)));
            Assert.Equal(new BillMonth(2026, 7), _service.GetInactiveFrom()["c2"]);
            Assert.Equal(1, updates);
            Assert.StartsWith("Jul 2026 · copied · current status set from this month's bills", _view.Heading);
        }

        [Fact]
        public async Task Copy_OlderMonth_KeepsCurrentStatus()
        {
            int updates = 0;
            _presenter.StatusesUpdated += () => updates++;
            _service.CreateFeeder("F", [E("A", "c1")]);
            _presenter.Load();
            _view.NextOpenFile = AsBilled(["c1", "10", "202607"]);
            await _presenter.OnChooseFileClickedAsync();
            await _presenter.OnCopyClickedAsync(7, 2026);
            _view.NextOpenFile = AsBilled(["c9", "10", "202606"]);
            await _presenter.OnChooseFileClickedAsync();
            await _presenter.OnCopyClickedAsync(6, 2026);
            Assert.True(_service.GetAllConsumers().Single().Entry.Active);
            Assert.Equal(1, updates);
            Assert.Equal("Inactive", _readings.GetMonth(new BillMonth(2026, 6)).Single().Status);
            Assert.StartsWith("Jun 2026 · copied · current status kept from Jul 2026 (newer month)", _view.Heading);
        }

        [Fact]
        public async Task Load_ShowsLatestSavedMonth_ExportableWithoutFile()
        {
            await ReadyAsync();
            await _presenter.OnCopyClickedAsync(7, 2026);
            var view = new FakeCopyView();
            var presenter = new CopyPresenter(view, _service, _readings, new AsBilledReader(), new FeederExporter(), () => AppSettings.Defaults, _remembered.Add, _log);
            presenter.Load();
            Assert.Equal((7, 2026), view.Month);
            Assert.Equal("Jul 2026 · saved copy · 2 feeders · 1 inactive (no bill) · 1 problem", view.Heading);
            Assert.True(view.ExportEnabled);
            Assert.False(view.CopyEnabled);

            view.NextSaveFile = TestFiles.TempPath(".xlsx");
            presenter.OnExportClicked(ExportStyle.OneWorkbook);
            using var wb = new XLWorkbook(view.NextSaveFile);
            Assert.Equal(1, wb.Worksheet(1).Cell("D3").GetDouble());
            Assert.Equal(0, wb.Worksheet(1).Cell("D4").GetDouble());
        }

        [Fact]
        public async Task MonthChanged_NothingSaved_DisablesExport()
        {
            await ReadyAsync();
            await _presenter.OnCopyClickedAsync(7, 2026);
            _presenter.OnMonthChanged(5, 2026);
            Assert.False(_view.ExportEnabled);
            Assert.Equal("Check the month, then click Copy kWh.", _view.ClearedMessage);
            _presenter.OnMonthChanged(7, 2026);
            Assert.True(_view.ExportEnabled);
            Assert.StartsWith("Jul 2026 · saved copy", _view.Heading);
        }

        public void Dispose() => _temp.Dispose();

        private static ConsumerEntry E(string? bus, string id) => new(bus, id);

        private static string AsBilled(params string?[][] rows) => TestFiles.WriteSpreadsheetMl([Header, .. rows]);

        private async Task ReadyAsync()
        {
            _service.CreateFeeder("New York Feeder", [E("A", "c1"), E("A", "c2")]);
            _service.CreateFeeder("Washington Feeder", [E("B", "c3")]);
            _presenter.Load();
            _view.NextOpenFile = AsBilled(["c1", "10", "202607"], ["c3", "5.5", "202607"], ["c3", "1", "202607"], [null, "1", "202607"]);
            await _presenter.OnChooseFileClickedAsync();
        }

        [Fact]
        public void Load_NoFeeders_Message()
        {
            _presenter.Load();
            Assert.Equal("Import a feeder list in the Feeders tab first.", _view.ClearedMessage);
            Assert.False(_view.CopyEnabled);
            Assert.False(_view.ExportEnabled);
            Assert.Equal(ExportStyle.OneWorkbook, _view.Style);
        }

        [Fact]
        public void Load_WithFeeders_Message()
        {
            _service.CreateFeeder("F", [E("A", "c1")]);
            _presenter.Load();
            Assert.Equal("Choose an As-Billed file to start.", _view.ClearedMessage);
        }

        [Fact]
        public async Task ChooseFile_Fatal_ShowsError()
        {
            _presenter.Load();
            _view.NextOpenFile = TestFiles.WriteSpreadsheetMl([["X"], ["1"]]);
            await _presenter.OnChooseFileClickedAsync();
            Assert.Contains("Missing column(s)", _view.Errors.Single());
            Assert.False(_view.CopyEnabled);
            Assert.Equal((false, (string?)null), _view.Busy[^1]);
        }

        [Fact]
        public async Task ChooseFile_PrefillsMonth_EnablesCopy()
        {
            await ReadyAsync();
            Assert.Equal((7, 2026), _view.Month);
            Assert.Equal(new BillMonth(2026, 7), _view.FileRead!.Value.Month);
            Assert.Equal(3, _view.FileRead.Value.Rows);
            Assert.Equal(1, _view.FileRead.Value.Skipped);
            Assert.True(_view.CopyEnabled);
            Assert.False(_view.ExportEnabled);
        }

        [Fact]
        public async Task Copy_NoMonth_Error()
        {
            await ReadyAsync();
            await _presenter.OnCopyClickedAsync(null, 2026);
            Assert.Equal(["Choose the month and year."], _view.Errors);
        }

        [Fact]
        public async Task Copy_MonthMismatch_ConfirmNo_Stops()
        {
            await ReadyAsync();
            _view.ConfirmAnswer = false;
            await _presenter.OnCopyClickedAsync(6, 2026);
            Assert.Equal(("Different month", "You chose Jun 2026, but the file contains Jul 2026. Use Jun 2026?"), _view.Confirms.Single());
            Assert.Null(_view.Totals);
        }

        [Fact]
        public async Task Copy_ShowsTotalsProblemsHeading_EnablesExport()
        {
            await ReadyAsync();
            await _presenter.OnCopyClickedAsync(7, 2026);
            Assert.Empty(_view.Confirms);
            Assert.Equal("Jul 2026 · copied · current status set from this month's bills · 2 feeders · 1 inactive (no bill) · 1 problem", _view.Heading);
            Assert.Equal(["New York Feeder", "Washington Feeder"], _view.Totals!.Select(t => t.Name));
            Assert.Equal(10, _view.Totals![0].TotalKwh);
            Assert.Equal(["c3"], _view.Problems!.Select(p => p.ConsumerId));
            Assert.True(_view.ExportEnabled);
        }

        [Fact]
        public async Task FeedersChanged_ShowsSavedMonthWithNewLists()
        {
            await ReadyAsync();
            await _presenter.OnCopyClickedAsync(7, 2026);
            _service.CreateFeeder("Boston Feeder", [E("Z", "c8")]);
            _presenter.OnFeedersChanged();
            Assert.Equal("Jul 2026 · saved copy · 3 feeders · 2 inactive (no bill) · 1 problem", _view.Heading);
            Assert.True(_view.ExportEnabled);
            Assert.True(_view.CopyEnabled);
        }

        [Fact]
        public async Task SaveProblems_WritesReport()
        {
            await ReadyAsync();
            await _presenter.OnCopyClickedAsync(7, 2026);
            _view.NextSaveFile = TestFiles.TempPath(".txt");
            _presenter.OnSaveProblemsClicked();
            Assert.Equal(("Text file|*.txt", "kWh copy problems - Jul 2026.txt"), _view.SavePrompts.Single());
            Assert.StartsWith("kWh copy for Jul 2026 from ", File.ReadAllText(_view.NextSaveFile));
        }

        [Fact]
        public async Task Export_Workbook_WritesFile_RemembersStyle()
        {
            await ReadyAsync();
            await _presenter.OnCopyClickedAsync(7, 2026);
            _view.NextSaveFile = TestFiles.TempPath(".xlsx");
            _presenter.OnExportClicked(ExportStyle.OneWorkbook);
            Assert.Equal(["Excel workbook|*.xlsx"], _view.SavePrompts.Select(p => p.Filter));
            Assert.Equal("Feeders - Jul 2026.xlsx", _view.SavePrompts[0].DefaultName);
            using (var wb = new XLWorkbook(_view.NextSaveFile))
                Assert.Equal(2, wb.Worksheets.Count);
            Assert.Equal([ExportStyle.OneWorkbook], _remembered);
            Assert.Equal(["Exported 2 feeders."], _view.Infos);
        }

        [Fact]
        public async Task Export_Separate_ExistingFiles_ConfirmNo_WritesNothing()
        {
            await ReadyAsync();
            await _presenter.OnCopyClickedAsync(7, 2026);
            string folder = TestFiles.TempFolder();
            File.WriteAllText(Path.Combine(folder, "New York Feeder - Jul 2026.xlsx"), "old");
            _view.NextFolder = folder;
            _view.ConfirmAnswer = false;
            _presenter.OnExportClicked(ExportStyle.SeparateFiles);
            Assert.Equal(("Replace files", "1 file already exists in this folder and will be replaced. Continue?"), _view.Confirms.Single());
            Assert.Equal("old", File.ReadAllText(Path.Combine(folder, "New York Feeder - Jul 2026.xlsx")));
            Assert.Single(Directory.GetFiles(folder));
        }

        [Fact]
        public async Task Export_Separate_WritesAll()
        {
            await ReadyAsync();
            await _presenter.OnCopyClickedAsync(7, 2026);
            string folder = TestFiles.TempFolder();
            _view.NextFolder = folder;
            _presenter.OnExportClicked(ExportStyle.SeparateFiles);
            Assert.Equal(2, Directory.GetFiles(folder).Length);
            Assert.Empty(_view.Confirms);
            Assert.Equal([ExportStyle.SeparateFiles], _remembered);
        }

        [Fact]
        public async Task Export_FileLocked_ShowsCloseInExcel()
        {
            await ReadyAsync();
            await _presenter.OnCopyClickedAsync(7, 2026);
            _view.NextSaveFile = TestFiles.TempPath(".xlsx");
            using (var locked = new FileStream(_view.NextSaveFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                _presenter.OnExportClicked(ExportStyle.OneWorkbook);
            Assert.Equal(["Could not save the file. If it is open in Excel, close it and try again."], _view.Errors);
            Assert.Single(_log.Entries);
        }
    }
}
