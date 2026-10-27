using ClosedXML.Excel;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Monitoring;
using FeederCopy.Core.Presenters.Monitoring;
using FeederCopy.Core.Presenters.Reports;
using FeederCopy.Core.Services.Data;
using FeederCopy.Core.Services.Export;

namespace FeederCopy.Tests.Presenters
{
    public sealed class ReportsPresenterTests : IDisposable
    {
        private readonly TempDatabase _temp = new();
        private readonly FeederService _feeders;
        private readonly ReadingService _readings;
        private readonly FeederTotalsCache _cache;
        private readonly FakeReportsView _view = new();
        private readonly FakeErrorLog _log = new();
        private readonly ReportsPresenter _presenter;
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "fc-reports-" + Guid.NewGuid().ToString("N"));

        public ReportsPresenterTests()
        {
            Directory.CreateDirectory(_folder);
            _feeders = new FeederService(_temp.Db);
            _readings = new ReadingService(_temp.Db);
            _cache = new FeederTotalsCache(_readings);
            _presenter = new ReportsPresenter(_view, _feeders, _readings, _cache, new MonitoringExporter(), _log);
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
            _readings.SaveMonth(new BillMonth(2026, 1), [new ReadingRecord("c1", "202601", 10, "Active"), new ReadingRecord("c2", "202601", null, "Inactive"), new ReadingRecord("c3", "202601", 5, "Active")]);
            _readings.SaveMonth(new BillMonth(2026, 2), [new ReadingRecord("c1", "202602", 12, "Active"), new ReadingRecord("c2", "202602", 3, "Active")]);
            _readings.SaveMonth(new BillMonth(2025, 12), [new ReadingRecord("c1", "202512", 8, "Active")]);
        }

        [Fact]
        public async Task Shown_NoMonths_ShowsEmpty()
        {
            await _presenter.OnShownAsync();
            Assert.Equal(MonitoringPresenter.NoMonths, _view.EmptyMessage);
            Assert.Equal(MonitoringTable.Measures.Count, _view.Measures.Count);
            Assert.False(_view.ExportEnabled);
        }

        [Fact]
        public async Task Shown_TotalKwhPerFeederPerMonth()
        {
            Seed();
            await _presenter.OnShownAsync();
            Assert.Equal(2026, _view.SelectedYear);
            Assert.Equal(0, _view.SelectedMeasure);
            Assert.Equal("Total kWh by feeder · 2026", _view.Title);
            Assert.Equal("Total", _view.TotalHeader);
            Assert.True(_view.Kwh);
            Assert.Equal(["Abuyog", "Bacon", MonitoringTable.AllFeeders], _view.Rows.Select(r => r.Feeder));
            Assert.Equal([10, 15], _view.Rows[1].Months.Take(2));
            Assert.Equal(25, _view.Rows[1].Total);
            Assert.Equal([15, 15], _view.Rows[2].Months.Take(2));
            Assert.Equal("2026 · 2 months transferred", _view.Summary);
            Assert.True(_view.ExportEnabled);
        }

        [Fact]
        public async Task FilterChanged_InactiveConsumers()
        {
            Seed();
            await _presenter.OnShownAsync();
            await _presenter.OnFilterChangedAsync(2026, 2);
            Assert.Equal("Inactive consumers by feeder · 2026", _view.Title);
            Assert.Equal("Average", _view.TotalHeader);
            Assert.False(_view.Kwh);
            Assert.Equal([1, 0], _view.Rows[1].Months.Take(2));
            Assert.Equal(0.5, _view.Rows[1].Total);
        }

        [Fact]
        public async Task FilterChanged_OtherYear()
        {
            Seed();
            await _presenter.OnShownAsync();
            await _presenter.OnFilterChangedAsync(2025, 0);
            Assert.Equal(2025, _view.SelectedYear);
            Assert.Equal(8, _view.Rows.Single(r => r.Feeder == "Bacon").Months[11]);
        }

        [Fact]
        public async Task Export_NoSheetTicked_ShowsError()
        {
            Seed();
            await _presenter.OnShownAsync();
            _presenter.OnExportClicked([]);
            Assert.Equal(["Tick at least one sheet to export."], _view.Errors);
            Assert.Empty(_view.SavePrompts);
        }

        [Fact]
        public async Task Export_OneSheetPerTickedMeasure()
        {
            Seed();
            await _presenter.OnShownAsync();
            _view.NextSaveFile = Path.Combine(_folder, "r.xlsx");
            _presenter.OnExportClicked([2, 0]);
            Assert.Equal(("Excel workbook|*.xlsx", "Feeder report 2026.xlsx"), _view.SavePrompts.Single());
            Assert.Equal(["Exported 2 sheets for 2026."], _view.Infos);
            using var workbook = new XLWorkbook(_view.NextSaveFile);
            Assert.Equal(["Total kWh", "Inactive consumers"], workbook.Worksheets.Select(w => w.Name));
            IXLWorksheet kwh = workbook.Worksheet("Total kWh");
            Assert.Equal("Feeder", kwh.Cell(2, 1).GetString());
            Assert.Equal("JAN", kwh.Cell(2, 2).GetString());
            Assert.Equal("DEC", kwh.Cell(2, 13).GetString());
            Assert.Equal("Total", kwh.Cell(2, 14).GetString());
            Assert.Equal("Bacon", kwh.Cell(4, 1).GetString());
            Assert.Equal(15, kwh.Cell(4, 3).GetDouble());
            Assert.True(kwh.Cell(4, 4).IsEmpty());
            Assert.Equal(25, kwh.Cell(4, 14).GetDouble());
            Assert.Equal(MonitoringTable.AllFeeders, kwh.Cell(5, 1).GetString());
            Assert.Equal("Average", workbook.Worksheet("Inactive consumers").Cell(2, 14).GetString());
        }
    }
}
