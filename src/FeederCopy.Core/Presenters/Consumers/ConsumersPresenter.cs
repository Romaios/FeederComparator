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

        public event Action? OrderChanged;

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

        public void OnSetActiveClicked(IReadOnlyList<string> consumerIds)
        {
            if (consumerIds.Count == 0 || !ConfirmMany(consumerIds, "active"))
                return;
            ChangeStatus(consumerIds, true, null);
        }

        public void OnSetInactiveClicked(IReadOnlyList<string> consumerIds, int? month, int year)
        {
            if (consumerIds.Count == 0)
                return;
            if (month is not (>= 1 and <= 12) || year is < 1900 or > 2100)
            {
                _view.ShowError("Choose the month the consumer became inactive.");
                return;
            }
            var from = new BillMonth(year, month.Value);
            if (!ConfirmMany(consumerIds, $"inactive from {from}"))
                return;
            ChangeStatus(consumerIds, false, from);
        }

        private bool ConfirmMany(IReadOnlyList<string> consumerIds, string status) =>
            consumerIds.Count == 1 || _view.Confirm("Set status", $"Set {consumerIds.Count:N0} consumers {status}?");

        public void OnConsumersDropped(IReadOnlyList<string> consumerIds, string targetConsumerId, bool after)
        {
            if (_consumers is null || consumerIds.Count == 0 || consumerIds.Contains(targetConsumerId))
                return;
            var byId = _consumers.ToDictionary(c => c.Entry.ConsumerId, StringComparer.Ordinal);
            if (!byId.TryGetValue(targetConsumerId, out var target) || consumerIds.Any(id => !byId.ContainsKey(id)))
                return;
            string targetBus = BusLabels.For(target.Entry.BusId);
            bool sameGroup = consumerIds.All(id =>
                string.Equals(byId[id].FeederName, target.FeederName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(BusLabels.For(byId[id].Entry.BusId), targetBus, StringComparison.OrdinalIgnoreCase));
            if (!sameGroup)
            {
                _view.ShowError($"Drop consumers inside their own bus ({target.FeederName}, bus {targetBus}). To move a consumer to another bus, change its Bus ID in the Feeders tab.");
                return;
            }

            bool moved = false;
            Run("Reordering consumers", () =>
            {
                FeederSummary feeder = _feeders.GetFeeders().First(f => string.Equals(f.Name, target.FeederName, StringComparison.OrdinalIgnoreCase));
                IReadOnlyList<ConsumerEntry> entries = _feeders.GetConsumers(feeder.Id);
                var moving = new HashSet<string>(consumerIds, StringComparer.Ordinal);
                List<ConsumerEntry> picked = entries.Where(e => moving.Contains(e.ConsumerId)).ToList();
                List<ConsumerEntry> rest = entries.Where(e => !moving.Contains(e.ConsumerId)).ToList();
                int at = rest.FindIndex(e => e.ConsumerId == targetConsumerId);
                if (at < 0)
                    return;
                rest.InsertRange(after ? at + 1 : at, picked);
                if (rest.Select(e => e.ConsumerId).SequenceEqual(entries.Select(e => e.ConsumerId)))
                    return;
                _feeders.SaveConsumers(new Dictionary<long, IReadOnlyList<ConsumerEntry>> { [feeder.Id] = rest });
                _consumers = null;
                Update();
                _view.SelectConsumers(picked.Select(e => e.ConsumerId).ToList());
                moved = true;
            });
            if (moved)
                OrderChanged?.Invoke();
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

        private void ChangeStatus(IReadOnlyList<string> consumerIds, bool active, BillMonth? inactiveFrom)
        {
            bool changed = false;
            Run("Setting consumer status", () =>
            {
                _feeders.SetActive(consumerIds, active, inactiveFrom);
                var ids = consumerIds.ToHashSet(StringComparer.Ordinal);
                if (_consumers is not null)
                {
                    for (int i = 0; i < _consumers.Count; i++)
                    {
                        if (ids.Contains(_consumers[i].Entry.ConsumerId))
                            _consumers[i] = (_consumers[i].FeederName, _consumers[i].Entry with { Active = active });
                    }
                }
                foreach (string id in ids)
                {
                    if (inactiveFrom is { } from)
                        _inactiveFrom[id] = from;
                    else
                        _inactiveFrom.Remove(id);
                }
                Update();
                if (consumerIds.Count == 1)
                    _view.SelectConsumer(consumerIds[0]);
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
