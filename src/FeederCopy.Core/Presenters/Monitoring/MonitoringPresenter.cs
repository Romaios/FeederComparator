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

        private readonly IMonitoringView _view;
        private readonly IFeederService _feeders;
        private readonly IReadingService _readings;
        private readonly IMonitoringExporter _exporter;
        private readonly IErrorLog _log;
        private int? _year;
        private bool _shown;
        private bool _stale = true;
        private int _request;
        private IReadOnlyList<MonthSummaryRow> _rows = [];

        public MonitoringPresenter(IMonitoringView view, IFeederService feeders, IReadingService readings, IMonitoringExporter exporter, IErrorLog log)
        {
            _view = view;
            _feeders = feeders;
            _readings = readings;
            _exporter = exporter;
            _log = log;
        }

        public void Load()
        {
            _year = null;
            MarkStale();
        }

        public Task OnShownAsync()
        {
            _shown = true;
            return _stale ? RefreshAsync() : Task.CompletedTask;
        }

        public void OnHidden() => _shown = false;

        public void OnDataChanged() => MarkStale();

        public Task OnYearChangedAsync(int year)
        {
            if (year == _year)
                return Task.CompletedTask;
            _year = year;
            return RefreshAsync();
        }

        public void OnExportClicked()
        {
            if (_year is not int year || _rows.Count == 0)
                return;
            string? path = _view.PickSaveFile("Excel workbook|*.xlsx", MonitoringTable.SummaryFileName(year));
            if (path is null)
                return;
            try
            {
                _exporter.WriteSummary(path, year, _rows);
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
                _ = RefreshAsync();
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
                (IReadOnlyList<int> years, int year, IReadOnlyList<MonthSummaryRow> rows) = await Task.Run(() => Read(wanted));
                if (request != _request)
                    return;
                _rows = rows;
                _year = years.Count > 0 ? year : null;
                _view.ShowYears(years, year);
                if (years.Count == 0)
                {
                    _view.ShowEmpty(NoMonths);
                    return;
                }
                if (rows.Count == 0)
                {
                    _view.ShowEmpty($"None of the readings saved for {year} belong to a feeder.");
                    return;
                }
                _view.ShowRows(rows, $"{year} · {Text.Count(rows.Count(r => r.IsTotal), "month", "months")} transferred");
                _view.SetExportEnabled(true);
            }
            catch (Exception ex)
            {
                if (request != _request)
                    return;
                _stale = true;
                _rows = [];
                _log.Error("Loading the monitoring", ex);
                _view.ShowEmpty("The monitoring could not be loaded.");
                _view.ShowError($"Could not load the monitoring. {ex.Message}");
            }
        }

        private (IReadOnlyList<int> Years, int Year, IReadOnlyList<MonthSummaryRow> Rows) Read(int? wanted)
        {
            IReadOnlyList<int> years = _readings.GetYears();
            if (years.Count == 0)
                return ([], 0, []);
            int year = wanted is int chosen && years.Contains(chosen) ? chosen : years[0];
            IReadOnlyList<string> feederNames = _feeders.GetFeeders().Select(f => f.Name).ToList();
            return (years, year, MonitoringTable.Summary(feederNames, _readings.GetFeederTotals(year)));
        }
    }
}
