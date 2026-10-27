using FeederCopy.Core.Models;
using FeederCopy.Core.Models.Monitoring;
using FeederCopy.Core.Services.Data;
using FeederCopy.Core.Services.Export;
using FeederCopy.Core.Services.Logging;

namespace FeederCopy.Core.Presenters.Monitoring
{
    public sealed class MonitoringPresenter
    {
        public const string NoMonths = "No months transferred yet. Transfer an As-Billed month in the Comparator tab.";
        public const string NothingTicked = "Tick Total kWh, Consumers active or Consumers inactive to see the monitoring.";

        private readonly IMonitoringView _view;
        private readonly IFeederService _feeders;
        private readonly IReadingService _readings;
        private readonly FeederTotalsCache _totalsCache;
        private readonly IMonitoringExporter _exporter;
        private readonly IErrorLog _log;
        private int? _year;
        private bool _shown;
        private bool _stale = true;
        private int _request;
        private Task _refresh = Task.CompletedTask;
        private IReadOnlyList<MonitoringMeasure> _measures = MonitoringTable.MonitoringMeasures;
        private IReadOnlyList<string> _feederNames = [];
        private IReadOnlyList<FeederMonthTotals> _totals = [];

        public MonitoringPresenter(IMonitoringView view, IFeederService feeders, IReadingService readings, FeederTotalsCache totalsCache, IMonitoringExporter exporter, IErrorLog log)
        {
            _view = view;
            _feeders = feeders;
            _readings = readings;
            _totalsCache = totalsCache;
            _exporter = exporter;
            _log = log;
        }

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

        public Task OnYearChangedAsync(int year)
        {
            if (year == _year)
                return Task.CompletedTask;
            _year = year;
            return _refresh = RefreshAsync();
        }

        public void OnMeasuresChanged(IReadOnlyList<int> measureIndexes)
        {
            _measures = measureIndexes
                .Where(i => i >= 0 && i < MonitoringTable.MonitoringMeasures.Count)
                .Distinct()
                .Order()
                .Select(i => MonitoringTable.MonitoringMeasures[i])
                .ToList();
            if (_year is int year && !_stale)
                ShowTables(year);
        }

        public void OnExportClicked()
        {
            if (_year is not int year || _totals.Count == 0)
                return;
            if (_measures.Count == 0)
            {
                _view.ShowError("Tick at least one item to export.");
                return;
            }
            string? path = _view.PickSaveFile("Excel workbook|*.xlsx", MonitoringTable.SummaryFileName(year));
            if (path is null)
                return;
            try
            {
                _exporter.WriteMonitoring(path, year, MonitoringTable.MonthsWithData(_totals), MonitoringTable.Sections(_feederNames, _totals, _measures));
                _view.ShowInfo($"Exported the monitoring for {year}.");
            }
            catch (Exception ex)
            {
                _log.Error("Exporting the monitoring", ex);
                _view.ShowError($"Could not export the monitoring. {ex.Message}");
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
            _view.ShowLoading("Loading the monitoring…");
            try
            {
                (IReadOnlyList<int> years, int year, IReadOnlyList<string> feederNames, IReadOnlyList<FeederMonthTotals> totals) = await Task.Run(() => Read(wanted));
                if (request != _request)
                    return;
                _feederNames = feederNames;
                _totals = totals;
                _year = years.Count > 0 ? year : null;
                _view.ShowYears(years, year);
                if (years.Count == 0)
                {
                    _view.ShowEmpty(NoMonths);
                    return;
                }
                ShowTables(year);
            }
            catch (Exception ex)
            {
                if (request != _request)
                    return;
                _stale = true;
                _totals = [];
                _log.Error("Loading the monitoring", ex);
                _view.ShowEmpty("The monitoring could not be loaded.");
                if (_shown)
                    _view.ShowError($"Could not load the monitoring. {ex.Message}");
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

        private void ShowTables(int year)
        {
            if (_totals.Count == 0)
            {
                _view.ShowEmpty($"None of the readings saved for {year} belong to a feeder.");
                _view.SetExportEnabled(false);
                return;
            }
            if (_measures.Count == 0)
            {
                _view.ShowEmpty(NothingTicked);
                _view.SetExportEnabled(false);
                return;
            }
            IReadOnlyList<int> months = MonitoringTable.MonthsWithData(_totals);
            _view.ShowSections(MonitoringTable.Sections(_feederNames, _totals, _measures), months,
                $"{year} · {Text.Count(months.Count, "month", "months")} transferred");
            _view.SetExportEnabled(true);
        }
    }
}
