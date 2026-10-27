using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Consumers;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Services.Data;
using FeederCopy.Core.Services.Logging;

namespace FeederCopy.Core.Presenters.Consumers
{
    public sealed class ConsumersPresenter
    {
        public const string Loading = "Loading consumers…";

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
        private bool _filtersPending;
        private int _request;
        private Task _refresh = Task.CompletedTask;
        private List<(string FeederName, ConsumerEntry Entry)>? _consumers;
        private Dictionary<string, BillMonth> _inactiveFrom = [];
        private IReadOnlyList<int> _years = [];
        private IReadOnlyList<string> _feederNames = [];
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

        private bool Stale => _consumers is null || _filtersStale || _yearReadings is null;

        public void Load()
        {
            _resetYear = true;
            MarkStale(feeders: true, readings: true);
            if (!_shown)
                _refresh = RefreshAsync();
        }

        public Task OnShownAsync()
        {
            _shown = true;
            if (!_refresh.IsCompleted)
            {
                _view.ShowLoading(Loading);
                return _refresh;
            }
            if (Stale)
                return _refresh = RefreshAsync();
            Run("Loading consumers", Show);
            return Task.CompletedTask;
        }

        public void OnHidden() => _shown = false;

        public void OnFeedersChanged() => MarkStale(feeders: true, readings: false);

        public void OnReadingsChanged() => MarkStale(feeders: false, readings: true);

        public Task OnFilterChangedAsync(string? feeder, bool active, int year, string? search, ConsumerProblem problem = ConsumerProblem.All)
        {
            _filter = new ConsumerFilter(feeder, active, search, problem);
            _year = year;
            if (Stale || _yearReadings?.Year != year || !_refresh.IsCompleted)
                return _refresh = RefreshAsync();
            Run("Filtering consumers", Show);
            return Task.CompletedTask;
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
                Reload();
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
            _request++;
            _refresh = _shown ? RefreshAsync() : Task.CompletedTask;
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
                Reload();
                if (consumerIds.Count == 1)
                    _view.SelectConsumer(consumerIds[0]);
                changed = true;
            });
            if (changed)
                StatusChanged?.Invoke();
        }

        private async Task RefreshAsync()
        {
            int request = ++_request;
            Wanted wanted = Want();
            if (_shown)
                _view.ShowLoading(Loading);
            try
            {
                Loaded loaded = await Task.Run(() => Fetch(wanted));
                if (request != _request)
                    return;
                Apply(loaded);
                if (_shown)
                    Show();
            }
            catch (Exception ex)
            {
                if (request != _request)
                    return;
                _log.Error("Loading consumers", ex);
                if (!_shown)
                    return;
                _view.ShowEmpty("The consumers could not be loaded.");
                _view.ShowError($"Could not load the consumers. {ex.Message}");
            }
        }

        private void Reload()
        {
            _request++;
            _refresh = Task.CompletedTask;
            Apply(Fetch(Want()));
            Show();
        }

        private Wanted Want() => new(_consumers is null, _filtersStale, _year, _resetYear, _yearReadings);

        private Loaded Fetch(Wanted wanted)
        {
            int currentYear = _currentMonth().Year;
            int year = wanted.Year ?? currentYear;
            IReadOnlyList<int>? years = null;
            if (wanted.Filters)
            {
                IReadOnlyList<int> saved = _readings.GetYears();
                years = ConsumerYearCalculator.Years(saved, currentYear);
                if (wanted.ResetYear || wanted.Year is null || !years.Contains(wanted.Year.Value))
                    year = ConsumerYearCalculator.DefaultYear(saved, currentYear);
            }

            var started = new List<Task>();
            Task<T>? Start<T>(bool needed, Func<T> read)
            {
                if (!needed)
                    return null;
                Task<T> task = Task.Run(read);
                started.Add(task);
                return task;
            }
            var consumers = Start(wanted.Consumers, () => _feeders.GetAllConsumers().ToList());
            var inactiveFrom = Start(wanted.Consumers, () => new Dictionary<string, BillMonth>(_feeders.GetInactiveFrom(), StringComparer.Ordinal));
            var feederNames = Start(wanted.Filters, () => (IReadOnlyList<string>)_feeders.GetFeeders().Select(f => f.Name).ToList());
            var yearReadings = Start(wanted.YearReadings?.Year != year, () => ConsumerYearCalculator.Index(_readings.GetYear(year), year));
            Task.WhenAll(started).GetAwaiter().GetResult();
            return new Loaded(consumers?.Result, inactiveFrom?.Result, years, feederNames?.Result, year, yearReadings?.Result);
        }

        private void Apply(Loaded loaded)
        {
            if (loaded.Consumers is not null && loaded.InactiveFrom is not null)
            {
                _consumers = loaded.Consumers;
                _inactiveFrom = loaded.InactiveFrom;
            }
            if (loaded.Years is not null && loaded.FeederNames is not null)
            {
                _years = loaded.Years;
                _feederNames = loaded.FeederNames;
                _filtersStale = false;
                _resetYear = false;
                _filtersPending = true;
            }
            _year = loaded.Year;
            if (loaded.YearReadings is not null)
                _yearReadings = loaded.YearReadings;
        }

        private void Show()
        {
            if (_consumers is null || _yearReadings is null)
                return;
            if (_filtersPending)
            {
                _view.ShowFilters(_feederNames, _years, _yearReadings.Year, _currentMonth());
                _filtersPending = false;
            }

            if (_consumers.Count == 0)
            {
                _view.ShowEmpty("No feeders yet. Import a feeder workbook in the Feeders tab.");
                return;
            }

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

        private sealed record Wanted(bool Consumers, bool Filters, int? Year, bool ResetYear, ConsumerYearReadings? YearReadings);

        private sealed record Loaded(
            List<(string FeederName, ConsumerEntry Entry)>? Consumers,
            Dictionary<string, BillMonth>? InactiveFrom,
            IReadOnlyList<int>? Years,
            IReadOnlyList<string>? FeederNames,
            int Year,
            ConsumerYearReadings? YearReadings);
    }
}
