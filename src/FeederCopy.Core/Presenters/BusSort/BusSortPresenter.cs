using FeederCopy.Core.Models;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Services.Data;
using FeederCopy.Core.Services.Logging;

namespace FeederCopy.Core.Presenters.BusSort
{
    public sealed class BusSortPresenter
    {
        private readonly IBusSortView _view;
        private readonly IFeederService _feeders;
        private readonly Func<AppSettings> _settings;
        private readonly Action<string> _saveTags;
        private readonly IErrorLog _log;
        private IReadOnlyList<FeederSummary> _all = [];
        private long? _selectedId;
        private bool _shown;
        private bool _stale = true;

        public BusSortPresenter(IBusSortView view, IFeederService feeders, Func<AppSettings> settings, Action<string> saveTags, IErrorLog log)
        {
            _view = view;
            _feeders = feeders;
            _settings = settings;
            _saveTags = saveTags;
            _log = log;
        }

        public event Action? Sorted;

        public void OnShown()
        {
            _shown = true;
            if (_stale)
                Run("Loading bus sorting", "Could not load the buses.", Update);
        }

        public void OnHidden() => _shown = false;

        public void OnFeedersChanged()
        {
            _stale = true;
            if (_shown)
                Run("Refreshing bus sorting", "Could not load the buses.", Update);
        }

        public void OnFeederSelected(long feederId)
        {
            if (_selectedId == feederId)
                return;
            _selectedId = feederId;
            Run("Loading feeder buses", "Could not load the buses.", Update);
        }

        public void OnTagToggled(string code, string tag, bool handPlaced) =>
            Run("Saving hand-placed bus tags", "Could not save the change.", () =>
            {
                _saveTags(BusTags.Toggle(_settings().HandPlacedBusTags, code, tag, handPlaced));
                Update();
            });

        public void OnSortClicked()
        {
            if (_all.FirstOrDefault(f => f.Id == _selectedId) is not { } feeder)
                return;
            bool sorted = false;
            Run("Sorting bus IDs", "Could not save the sorted list.", () =>
            {
                IReadOnlyList<BusTagRule> tags = BusTags.ParseRules(_settings().HandPlacedBusTags);
                IReadOnlyList<ConsumerEntry> current = FeederLayout.Arrange(_feeders.GetConsumers(feeder.Id));
                List<ConsumerEntry> result = BusSorter.Sort(current, tags);
                int moved = BusSorter.MovedRows(current, result);
                if (moved == 0)
                {
                    _view.ShowInfo($"The bus IDs in {feeder.Name} are already in order.");
                    return;
                }
                string tagText = tags.Count == 0 ? string.Empty : " Buses with hand-placed tags stay under their parent bus in the order you placed them.";
                if (!_view.Confirm("Sort bus IDs", $"Sort the bus IDs in {feeder.Name}? {moved:N0} of {current.Count:N0} rows will move.{tagText}"))
                    return;
                _feeders.SaveConsumers(new Dictionary<long, IReadOnlyList<ConsumerEntry>> { [feeder.Id] = result });
                Update();
                sorted = true;
            });
            if (sorted)
                Sorted?.Invoke();
        }

        private void Update()
        {
            _stale = false;
            _all = _feeders.GetFeeders();
            if (_all.Count == 0)
            {
                _selectedId = null;
                _view.ShowEmpty("No feeders yet. Import a feeder workbook in the Feeders tab.");
                return;
            }
            FeederSummary feeder = _all.FirstOrDefault(f => f.Id == _selectedId) ?? _all[0];
            _selectedId = feeder.Id;
            _view.ShowFeeders(_all, feeder.Id);

            IReadOnlyList<BusTagRule> rules = BusTags.ParseRules(_settings().HandPlacedBusTags);
            IReadOnlyList<ConsumerEntry> entries = _feeders.GetConsumers(feeder.Id);
            List<ConsumerEntry> sorted = BusSorter.Sort(entries, rules);
            int moved = BusSorter.MovedRows(FeederLayout.Arrange(entries), sorted);
            IReadOnlyList<BusBlockRow> blocks = BusBlocks.Plan(entries, sorted, rules);
            IReadOnlyList<BusTagRow> tags = BusTags.Discover(entries.Where(e => e.BusId is not null).Select(e => (feeder.Name, e.BusId!)), rules);
            string order = moved == 0 ? "already in order" : $"{moved:N0} of {entries.Count:N0} rows move when sorted";
            string summary = $"{Text.Count(FeederLayout.Buses(entries).Count, "bus", "buses")}  ·  {order}";
            _view.ShowPlan(summary, blocks, tags, moved > 0);
        }

        private void Run(string context, string message, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _log.Error(context, ex);
                _view.ShowError($"{message} {ex.Message}");
            }
        }
    }
}
