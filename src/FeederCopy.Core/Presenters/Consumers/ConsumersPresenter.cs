using FeederCopy.Core.Models;
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
        private readonly Func<int> _currentYear;
        private readonly IErrorLog _log;
        private ConsumerFilter _filter = ConsumerFilter.All;
        private int? _year;

        public ConsumersPresenter(IConsumersView view, IFeederService feeders, IReadingService readings, Func<int> currentYear, IErrorLog log)
        {
            _view = view;
            _feeders = feeders;
            _readings = readings;
            _currentYear = currentYear;
            _log = log;
        }

        public event Action? StatusChanged;

        public void Load() => Run("Loading consumers", () => Reload(resetYear: true));

        public void OnDataChanged() => Run("Refreshing consumers", () => Reload(resetYear: false));

        public void OnFilterChanged(string? feeder, bool? active, int year, string? search)
        {
            _filter = new ConsumerFilter(feeder, active, search);
            _year = year;
            Run("Filtering consumers", Refresh);
        }

        public void OnSetActiveClicked(string consumerId, bool active)
        {
            bool changed = false;
            Run("Setting consumer status", () =>
            {
                _feeders.SetActive(consumerId, active);
                Refresh();
                _view.SelectConsumer(consumerId);
                changed = true;
            });
            if (changed)
                StatusChanged?.Invoke();
        }

        private void Reload(bool resetYear)
        {
            IReadOnlyList<int> saved = _readings.GetYears();
            IReadOnlyList<int> years = ConsumerYearCalculator.Years(saved, _currentYear());
            if (resetYear || _year is null || !years.Contains(_year.Value))
                _year = ConsumerYearCalculator.DefaultYear(saved, _currentYear());
            IReadOnlyList<string> feederNames = _feeders.GetFeeders().Select(f => f.Name).ToList();
            _view.ShowFilters(feederNames, years, _year.Value);
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
            int year = _year ?? _currentYear();
            IReadOnlyList<ConsumerYearRow> rows = ConsumerYearCalculator.Build(consumers, _readings.GetYear(year), year, _filter);
            int active = rows.Count(r => r.Active);
            string summary = $"{Text.Count(rows.Count, "consumer", "consumers")} · {active:N0} active · {rows.Count - active:N0} inactive";
            _view.ShowRows(rows, summary);
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
