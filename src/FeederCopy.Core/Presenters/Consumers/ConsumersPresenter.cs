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

        public ConsumersPresenter(IConsumersView view, IFeederService feeders, IReadingService readings, Func<BillMonth> currentMonth, IErrorLog log)
        {
            _view = view;
            _feeders = feeders;
            _readings = readings;
            _currentMonth = currentMonth;
            _log = log;
        }

        public event Action? StatusChanged;

        public void Load() => Run("Loading consumers", () => Reload(resetYear: true));

        public void OnDataChanged() => Run("Refreshing consumers", () => Reload(resetYear: false));

        public void OnFilterChanged(string? feeder, bool active, int year, string? search)
        {
            _filter = new ConsumerFilter(feeder, active, search);
            _year = year;
            Run("Filtering consumers", Refresh);
        }

        public void OnSetActiveClicked(string consumerId) =>
            ChangeStatus(consumerId, () => _feeders.SetActive(consumerId, true));

        public void OnSetInactiveClicked(string consumerId, int? month, int year)
        {
            if (month is not (>= 1 and <= 12) || year is < 1900 or > 2100)
            {
                _view.ShowError("Choose the month the consumer became inactive.");
                return;
            }
            var from = new BillMonth(year, month.Value);
            ChangeStatus(consumerId, () => _feeders.SetActive(consumerId, false, from));
        }

        private void ChangeStatus(string consumerId, Action save)
        {
            bool changed = false;
            Run("Setting consumer status", () =>
            {
                save();
                Refresh();
                _view.SelectConsumer(consumerId);
                changed = true;
            });
            if (changed)
                StatusChanged?.Invoke();
        }

        private void Reload(bool resetYear)
        {
            int currentYear = _currentMonth().Year;
            IReadOnlyList<int> saved = _readings.GetYears();
            IReadOnlyList<int> years = ConsumerYearCalculator.Years(saved, currentYear);
            if (resetYear || _year is null || !years.Contains(_year.Value))
                _year = ConsumerYearCalculator.DefaultYear(saved, currentYear);
            IReadOnlyList<string> feederNames = _feeders.GetFeeders().Select(f => f.Name).ToList();
            _view.ShowFilters(feederNames, years, _year.Value, _currentMonth());
            Refresh();
        }

        private void Refresh()
        {
            IReadOnlyList<(string FeederName, ConsumerEntry Entry)> consumers = _feeders.GetAllConsumers();
            if (consumers.Count == 0)
            {
                _view.ShowEmpty("No feeders yet. Import a feeder workbook in the Feeders tab.");
                return;
            }
            int year = _year ?? _currentMonth().Year;
            IReadOnlyList<ConsumerYearRow> all = ConsumerYearCalculator.Build(
                consumers, _readings.GetYear(year), year, _filter with { Active = null }, _feeders.GetInactiveFrom());
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
