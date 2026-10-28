using FeederCopy.Core.Models;
using FeederCopy.Core.Models.Monitoring;
using FeederCopy.Core.Presenters.Monitoring;
using FeederCopy.Core.Services.Data;
using FeederCopy.Core.Services.Export;
using FeederCopy.Core.Services.Logging;

namespace FeederCopy.Core.Presenters.Reports
{
    public sealed class ReportsPresenter
    {
        private readonly IReportsView _view;
        private readonly IFeederService _feeders;
        private readonly IReadingService _readings;
        private readonly FeederTotalsCache _totalsCache;
        private readonly IMonitoringExporter _exporter;
        private readonly IErrorLog _log;
        private int? _year;
        private MonitoringMeasure _measure = MonitoringMeasure.TotalKwh;
        private bool _shown;
        private bool _stale = true;
        private int _request;
        private Task _refresh = Task.CompletedTask;
        private IReadOnlyList<string> _feederNames = [];
        private IReadOnlyList<FeederMonthTotals> _totals = [];

        public ReportsPresenter(IReportsView view, IFeederService feeders, IReadingService readings, FeederTotalsCache totalsCache, IMonitoringExporter exporter, IErrorLog log)
        {
            _view = view;
            _feeders = feeders;
            _readings = readings;
            _totalsCache = totalsCache;
            _exporter = exporter;
            _log = log;
        }

        private static IReadOnlyList<string> MeasureLabels { get; } = MonitoringTable.Measures.Select(MonitoringTable.Label).ToList();

        private int MeasureIndex => MonitoringTable.Measures.ToList().IndexOf(_measure);

        public void Load()
        {
            _year = null;
            _stale = true;
            _refresh = RefreshAsync();
        }

        public Task OnShownAsync()
        {
            _shown = true;
            if (_stale)
                _refresh = RefreshAsync();
            return _refresh;
        }

        public void OnHidden() => _shown = false;

        public void OnDataChanged() => MarkStale();

        public Task OnFilterChangedAsync(int year, int measureIndex)
        {
            if (measureIndex < 0 || measureIndex >= MonitoringTable.Measures.Count)
                return Task.CompletedTask;
            MonitoringMeasure measure = MonitoringTable.Measures[measureIndex];
            if (year == _year && measure == _measure)
                return Task.CompletedTask;
            bool reload = year != _year;
            _year = year;
            _measure = measure;
            if (reload)
                return _refresh = RefreshAsync();
            ShowTable(year);
            return Task.CompletedTask;
        }

        public void OnExportClicked(IReadOnlyList<int> measureIndexes)
        {
            if (_year is not int year || _totals.Count == 0)
                return;
            List<MonitoringMeasure> measures = measureIndexes
                .Where(i => i >= 0 && i < MonitoringTable.Measures.Count)
                .Distinct()
                .Order()
                .Select(i => MonitoringTable.Measures[i])
                .ToList();
            if (measures.Count == 0)
            {
                _view.ShowError("Tick at least one sheet to export.");
                return;
            }
            string? path = _view.PickSaveFile("Excel workbook|*.xlsx", MonitoringTable.ReportFileName(year));
            if (path is null)
                return;
            try
            {
                _exporter.WriteReport(path, year, measures.Select(m => (m, MonitoringTable.ByMonth(_feederNames, _totals, m))).ToList());
                _view.ShowInfo($"Exported {Text.Count(measures.Count, "sheet", "sheets")} for {year}.");
            }
            catch (Exception ex)
            {
                _log.Error("Exporting the feeder report", ex);
                _view.ShowError($"Could not export the report. {ex.Message}");
            }
        }

        private void MarkStale()
        {
            _stale = true;
            if (_shown)
                _refresh = RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            int request = ++_request;
            _stale = false;
            int? wanted = _year;
            _view.SetExportEnabled(false);
            _view.ShowLoading("Loading the report…");
            try
            {
                (IReadOnlyList<int> years, int year, IReadOnlyList<string> feederNames, IReadOnlyList<FeederMonthTotals> totals) = await Task.Run(() => Read(wanted));
                if (request != _request)
                    return;
                _feederNames = feederNames;
                _totals = totals;
                _year = years.Count > 0 ? year : null;
                _view.ShowFilters(years, year, MeasureLabels, MeasureIndex);
                if (years.Count == 0)
                {
                    _view.ShowEmpty(MonitoringPresenter.NoMonths);
                    return;
                }
                ShowTable(year);
            }
            catch (Exception ex)
            {
                if (request != _request)
                    return;
                _stale = true;
                _totals = [];
                _log.Error("Loading the feeder report", ex);
                _view.ShowEmpty("The report could not be loaded.");
                if (_shown)
                    _view.ShowError($"Could not load the report. {ex.Message}");
            }
        }

        private (IReadOnlyList<int> Years, int Year, IReadOnlyList<string> FeederNames, IReadOnlyList<FeederMonthTotals> Totals) Read(int? wanted)
        {
            IReadOnlyList<int> years = _readings.GetYears();
            if (years.Count == 0)
                return ([], 0, [], []);
            int year = wanted is int chosen && years.Contains(chosen) ? chosen : years[0];
            IReadOnlyList<string> feederNames = _feeders.GetFeeders().Select(f => f.Name).ToList();
            return (years, year, feederNames, _totalsCache.Get(year));
        }

        private void ShowTable(int year)
        {
            if (_totals.Count == 0)
            {
                _view.ShowEmpty($"None of the readings saved for {year} belong to a feeder.");
                _view.SetExportEnabled(false);
                return;
            }
            IReadOnlyList<FeederMonthsRow> rows = MonitoringTable.ByMonth(_feederNames, _totals, _measure);
            int months = _totals.Select(t => t.Month).Distinct().Count();
            _view.ShowRows($"{MonitoringTable.Label(_measure)} by feeder · {year}", MonitoringTable.TotalHeader, MonitoringTable.IsKwh(_measure),
                rows, $"{year} · {Text.Count(months, "month", "months")} transferred");
            _view.SetExportEnabled(true);
        }
    }
}
