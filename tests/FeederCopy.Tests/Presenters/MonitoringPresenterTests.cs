using ClosedXML.Excel;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Monitoring;
using FeederCopy.Core.Presenters.Monitoring;
using FeederCopy.Core.Services.Data;
using FeederCopy.Core.Services.Export;

namespace FeederCopy.Tests.Presenters
{
    public sealed class MonitoringPresenterTests : IDisposable
    {
        private readonly TempDatabase _temp = new();
        private readonly FeederService _feeders;
        private readonly ReadingService _readings;
        private readonly FakeMonitoringView _view = new();
        private readonly FakeErrorLog _log = new();
        private readonly MonitoringPresenter _presenter;
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "fc-monitoring-" + Guid.NewGuid().ToString("N"));

        public MonitoringPresenterTests()
        {
            Directory.CreateDirectory(_folder);
            _feeders = new FeederService(_temp.Db);
            _readings = new ReadingService(_temp.Db);
            _presenter = new MonitoringPresenter(_view, _feeders, _readings, new MonitoringExporter(), _log);
        }

        public void Dispose()
        {
            _temp.Dispose();
            Directory.Delete(_folder, true);
        }

        private void Seed()
        {
            _feeders.CreateFeeder("Bacon", [new ConsumerEntry("B", "c1"), new ConsumerEntry("B", "c2")]);
            _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c3")]);
            _readings.SaveMonth(new BillMonth(2026, 6), [new ReadingRecord("c1", "202606", 10, "Active"), new ReadingRecord("c2", "202606", null, "Inactive"), new ReadingRecord("c3", "202606", 5, "Active")]);
            _readings.SaveMonth(new BillMonth(2026, 7), [new ReadingRecord("c1", "202607", 12, "Active"), new ReadingRecord("c2", "202607", 3, "Active")]);
            _readings.SaveMonth(new BillMonth(2025, 12), [new ReadingRecord("c1", "202512", 8, "Active")]);
        }

        [Fact]
        public async Task Shown_NoMonths_ShowsEmpty()
        {
            await _presenter.OnShownAsync();
            Assert.Equal(MonitoringPresenter.NoMonths, _view.EmptyMessage);
            Assert.Empty(_view.Years);
            Assert.False(_view.ExportEnabled);
        }

        [Fact]
        public async Task Shown_ShowsLatestYearMonthByMonth()
        {
            Seed();
            await _presenter.OnShownAsync();
            Assert.Equal([2026, 2025], _view.Years);
            Assert.Equal(2026, _view.SelectedYear);
            Assert.Equal("2026 · 2 months transferred", _view.Summary);
            Assert.Equal(
            [
                new MonthSummaryRow(new BillMonth(2026, 6), "Abuyog", 5, 1, 0, false),
                new MonthSummaryRow(new BillMonth(2026, 6), "Bacon", 10, 1, 1, false),
                new MonthSummaryRow(new BillMonth(2026, 6), MonitoringTable.AllFeeders, 15, 2, 1, true),
                new MonthSummaryRow(new BillMonth(2026, 7), "Bacon", 15, 2, 0, false),
                new MonthSummaryRow(new BillMonth(2026, 7), MonitoringTable.AllFeeders, 15, 2, 0, true),
            ], _view.Rows);
            Assert.True(_view.ExportEnabled);
        }

        [Fact]
        public async Task YearChanged_ShowsThatYear()
        {
            Seed();
            await _presenter.OnShownAsync();
            await _presenter.OnYearChangedAsync(2025);
            Assert.Equal(2025, _view.SelectedYear);
            Assert.Equal(["Bacon", MonitoringTable.AllFeeders], _view.Rows.Select(r => r.Feeder));
        }

        [Fact]
        public async Task DataChanged_WhileHidden_RefreshesOnNextShow()
        {
            await _presenter.OnShownAsync();
            _presenter.OnHidden();
            Seed();
            _presenter.OnDataChanged();
            Assert.Equal(MonitoringPresenter.NoMonths, _view.EmptyMessage);
            await _presenter.OnShownAsync();
            Assert.Null(_view.EmptyMessage);
            Assert.Equal(5, _view.Rows.Count);
        }

        [Fact]
        public async Task Shown_DatabaseFails_ShowsErrorAndRetriesNextTime()
        {
            var broken = new Database(DatabaseConnection.Empty);
            var presenter = new MonitoringPresenter(_view, new FeederService(broken), new ReadingService(broken), new MonitoringExporter(), _log);
            await presenter.OnShownAsync();
            Assert.Equal("The monitoring could not be loaded.", _view.EmptyMessage);
            Assert.StartsWith("Could not load the monitoring.", _view.Errors.Single());
            Assert.Single(_log.Entries);
            presenter.OnHidden();
            await presenter.OnShownAsync();
            Assert.Equal(2, _view.Errors.Count);
        }

        [Fact]
        public async Task Shown_ShowsLoadingFirst()
        {
            Seed();
            await _presenter.OnShownAsync();
            Assert.Equal("Loading the monitoring…", _view.LoadingMessage);
        }

        [Fact]
        public async Task Export_WritesTheSummarySheet()
        {
            Seed();
            await _presenter.OnShownAsync();
            _view.NextSaveFile = Path.Combine(_folder, "m.xlsx");
            _presenter.OnExportClicked();
            Assert.Equal(("Excel workbook|*.xlsx", "Monitoring 2026.xlsx"), _view.SavePrompts.Single());
            Assert.Equal(["Exported the monitoring for 2026."], _view.Infos);
            using var workbook = new XLWorkbook(_view.NextSaveFile);
            IXLWorksheet sheet = workbook.Worksheet("Monitoring");
            Assert.Equal("Monitoring 2026", sheet.Cell(1, 1).GetString());
            Assert.Equal("Inactive", sheet.Cell(2, 5).GetString());
            Assert.Equal("Jun 2026", sheet.Cell(3, 1).GetString());
            Assert.Equal("Abuyog", sheet.Cell(3, 2).GetString());
            Assert.Equal(MonitoringTable.AllFeeders, sheet.Cell(5, 2).GetString());
            Assert.Equal(15, sheet.Cell(5, 3).GetDouble());
            Assert.True(sheet.Cell(5, 2).Style.Font.Bold);
        }

        [Fact]
        public async Task Export_Cancelled_WritesNothing()
        {
            Seed();
            await _presenter.OnShownAsync();
            _presenter.OnExportClicked();
            Assert.Empty(_view.Infos);
            Assert.Empty(Directory.GetFiles(_folder));
        }

        [Fact]
        public async Task Export_FileLocked_ShowsErrorAndLogs()
        {
            Seed();
            await _presenter.OnShownAsync();
            _view.NextSaveFile = Path.Combine(_folder, "locked.xlsx");
            using (File.Create(_view.NextSaveFile))
                _presenter.OnExportClicked();
            Assert.StartsWith("Could not export the monitoring.", _view.Errors.Single());
            Assert.Single(_log.Entries);
        }
    }
}
