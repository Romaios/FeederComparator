using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Consumers;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Services.Data;
using FeederCopy.Core.Services.Logging;

namespace FeederCopy.Core.Presenters.Consumers
{
    public sealed class ConsumersPresenter
    {
        private readonly IConsumersView _view;
        private readonly IFeederService _feeders;
        private readonly IReadingService _readings;
        private readonly Func<BillMonth> _currentMonth;
        private readonly IErrorLog _log;
        private ConsumerFilter _filter = new(null, true, null);
        private int? _year;
        private bool _shown;
        private bool _filtersStale = true;
        private bool _resetYear = true;
        private List<(string FeederName, ConsumerEntry Entry)>? _consumers;
        private Dictionary<string, BillMonth> _inactiveFrom = [];
        private ConsumerYearReadings? _yearReadings;

        public ConsumersPresenter(IConsumersView view, IFeederService feeders, IReadingService readings, Func<BillMonth> currentMonth, IErrorLog log)
        {
            _view = view;
            _feeders = feeders;
            _readings = readings;
            _currentMonth = currentMonth;
            _log = log;
        }

        public event Action? StatusChanged;

        public void Load()
        {
            _resetYear = true;
            MarkStale(feeders: true, readings: true);
        }

        public void OnShown()
        {
            _shown = true;
            Run("Loading consumers", Update);
        }

        public void OnHidden() => _shown = false;

        public void OnFeedersChanged() => MarkStale(feeders: true, readings: false);

        public void OnReadingsChanged() => MarkStale(feeders: false, readings: true);

        public void OnFilterChanged(string? feeder, bool active, int year, string? search)
        {
            _filter = new ConsumerFilter(feeder, active, search);
            _year = year;
            Run("Filtering consumers", Update);
        }

        public void OnSetActiveClicked(string consumerId) =>
            ChangeStatus(consumerId, true, null);

        public void OnSetInactiveClicked(string consumerId, int? month, int year)
        {
            if (month is not (>= 1 and <= 12) || year is < 1900 or > 2100)
            {
                _view.ShowError("Choose the month the consumer became inactive.");
                return;
            }
            ChangeStatus(consumerId, false, new BillMonth(year, month.Value));
        }

        private void MarkStale(bool feeders, bool readings)
        {
            if (feeders)
                _consumers = null;
            if (readings)
                _yearReadings = null;
            _filtersStale = true;
            if (_shown)
                Run("Refreshing consumers", Update);
        }

        private void ChangeStatus(string consumerId, bool active, BillMonth? inactiveFrom)
        {
            bool changed = false;
            Run("Setting consumer status", () =>
            {
                _feeders.SetActive(consumerId, active, inactiveFrom);
                if (_consumers is not null)
                {
                    int index = _consumers.FindIndex(c => c.Entry.ConsumerId == consumerId);
                    if (index >= 0)
                        _consumers[index] = (_consumers[index].FeederName, _consumers[index].Entry with { Active = active });
                }
                if (inactiveFrom is { } from)
                    _inactiveFrom[consumerId] = from;
                else
                    _inactiveFrom.Remove(consumerId);
                Update();
                _view.SelectConsumer(consumerId);
                changed = true;
            });
            if (changed)
                StatusChanged?.Invoke();
        }

        private void Update()
        {
            if (_consumers is null)
            {
                _consumers = _feeders.GetAllConsumers().ToList();
                _inactiveFrom = new Dictionary<string, BillMonth>(_feeders.GetInactiveFrom(), StringComparer.Ordinal);
            }

            if (_filtersStale)
            {
                int currentYear = _currentMonth().Year;
                IReadOnlyList<int> saved = _readings.GetYears();
                IReadOnlyList<int> years = ConsumerYearCalculator.Years(saved, currentYear);
                if (_resetYear || _year is null || !years.Contains(_year.Value))
                    _year = ConsumerYearCalculator.DefaultYear(saved, currentYear);
                _resetYear = false;
                IReadOnlyList<string> feederNames = _feeders.GetFeeders().Select(f => f.Name).ToList();
                _view.ShowFilters(feederNames, years, _year.Value, _currentMonth());
                _filtersStale = false;
            }

            if (_consumers.Count == 0)
            {
                _view.ShowEmpty("No feeders yet. Import a feeder workbook in the Feeders tab.");
                return;
            }

            int year = _year ?? _currentMonth().Year;
            if (_yearReadings is null || _yearReadings.Year != year)
                _yearReadings = ConsumerYearCalculator.Index(_readings.GetYear(year), year);
            IReadOnlyList<ConsumerYearRow> all = ConsumerYearCalculator.Build(_consumers, _yearReadings, _filter with { Active = null }, _inactiveFrom);
            int active = all.Count(r => r.Active);
            bool showActive = _filter.Active ?? true;
            IReadOnlyList<ConsumerYearRow> rows = all.Where(r => r.Active == showActive).ToList();
            string summary = $"{rows.Count:N0} {(showActive ? "active" : "inactive")} {(rows.Count == 1 ? "consumer" : "consumers")}";
            _view.ShowRows(rows, summary, active, all.Count - active);
        }

        private void Run(string context, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _log.Error(context, ex);
                _view.ShowError($"Could not load the consumers. {ex.Message}");
            }
        }
    }
}
