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
        private IReadOnlyList<ConsumerEntry> _saved = [];
        private List<BusBlock> _draft = [];
        private Dictionary<string, string?> _tagCache = new(StringComparer.OrdinalIgnoreCase);
        private IReadOnlyList<BusTagRow>? _tagRows;

        public BusSortPresenter(IBusSortView view, IFeederService feeders, Func<AppSettings> settings, Action<string> saveTags, IErrorLog log)
        {
            _view = view;
            _feeders = feeders;
            _settings = settings;
            _saveTags = saveTags;
            _log = log;
        }

        public event Action? Sorted;

        private FeederSummary? Selected => _all.FirstOrDefault(f => f.Id == _selectedId);

        private int MovedRows => BusSorter.MovedRows(_saved, BusBlocks.Flatten(_draft));

        public void OnShown()
        {
            _shown = true;
            if (_stale)
                Run("Loading bus sorting", "Could not load the buses.", Reload);
        }

        public void OnHidden() => _shown = false;

        public void OnFeedersChanged()
        {
            _stale = true;
            if (_shown)
                Run("Refreshing bus sorting", "Could not load the buses.", Reload);
        }

        public void OnFeederSelected(long feederId)
        {
            if (_selectedId == feederId)
                return;
            if (MovedRows > 0 && Selected is { } current
                && !_view.Confirm("Unsaved bus order", $"The new bus order for {current.Name} isn't saved. Discard it?"))
            {
                _view.ShowFeeders(_all, current.Id);
                return;
            }
            _selectedId = feederId;
            Run("Loading feeder buses", "Could not load the buses.", Reload);
        }

        public void OnBlocksMoved(IReadOnlyList<int> indexes, int insertAt)
        {
            (List<BusBlock> blocks, IReadOnlyList<int> moved) = BusBlocks.Move(_draft, indexes.ToList(), insertAt);
            if (moved.Count == 0)
                return;
            _draft = blocks;
            Show();
            _view.SelectBlocks(moved);
        }

        public void OnMoveUpClicked(IReadOnlyList<int> indexes)
        {
            if (indexes.Count > 0 && indexes.Min() > 0)
                OnBlocksMoved(indexes, indexes.Min() - 1);
        }

        public void OnMoveDownClicked(IReadOnlyList<int> indexes)
        {
            if (indexes.Count > 0 && indexes.Max() < _draft.Count - 1)
                OnBlocksMoved(indexes, indexes.Max() + 2);
        }

        public void OnSortClicked()
        {
            if (Selected is not { } feeder)
                return;
            List<BusBlock> sorted = BusBlocks.Sort(_draft, Rules());
            if (BusSorter.MovedRows(BusBlocks.Flatten(_draft), BusBlocks.Flatten(sorted)) == 0)
            {
                _view.ShowInfo($"The bus IDs in {feeder.Name} are already in number order.");
                return;
            }
            _draft = sorted;
            Show();
        }

        public void OnUndoClicked()
        {
            _draft = BusBlocks.Split(_saved);
            Show();
        }

        public void OnSaveClicked()
        {
            if (Selected is not { } feeder)
                return;
            int moved = MovedRows;
            if (moved == 0)
                return;
            if (!_view.Confirm("Save bus order", $"Save the new bus order for {feeder.Name}? {moved:N0} of {_saved.Count:N0} rows will move."))
                return;
            bool saved = false;
            Run("Saving bus order", "Could not save the bus order.", () =>
            {
                _feeders.SaveConsumers(new Dictionary<long, IReadOnlyList<ConsumerEntry>> { [feeder.Id] = BusBlocks.Flatten(_draft) });
                Reload();
                saved = true;
            });
            if (saved)
                Sorted?.Invoke();
        }

        public void OnTagToggled(string code, string tag, bool handPlaced) =>
            Run("Saving hand-placed bus tags", "Could not save the change.", () =>
            {
                _saveTags(BusTags.Toggle(_settings().HandPlacedBusTags, code, tag, handPlaced));
                _tagCache = new(StringComparer.OrdinalIgnoreCase);
                _tagRows = null;
                Show();
            });

        private IReadOnlyList<BusTagRule> Rules() => BusTags.ParseRules(_settings().HandPlacedBusTags);

        private void Reload()
        {
            _stale = false;
            _all = _feeders.GetFeeders();
            if (_all.Count == 0)
            {
                _selectedId = null;
                _saved = [];
                _draft = [];
                _view.ShowEmpty("No feeders yet. Import a feeder workbook in the Feeders tab.");
                return;
            }
            FeederSummary feeder = Selected ?? _all[0];
            _selectedId = feeder.Id;
            _view.ShowFeeders(_all, feeder.Id);
            _saved = _feeders.GetConsumers(feeder.Id);
            _draft = BusBlocks.Split(_saved);
            _tagCache = new(StringComparer.OrdinalIgnoreCase);
            _tagRows = null;
            Show();
        }

        private void Show()
        {
            if (Selected is not { } feeder)
                return;
            IReadOnlyList<BusTagRule> rules = Rules();
            string? TagOf(string bus)
            {
                if (!_tagCache.TryGetValue(bus, out string? tag))
                    _tagCache[bus] = tag = BusTags.MatchedTag(bus, rules);
                return tag;
            }
            IReadOnlyList<BusBlockRow> rows = BusBlocks.Rows(_saved, _draft, TagOf);
            _tagRows ??= BusTags.Discover(_saved.Where(e => e.BusId is not null).Select(e => (feeder.Name, e.BusId!)), rules);
            int moved = MovedRows;
            string state = moved == 0 ? "saved order" : $"not saved: {moved:N0} of {_saved.Count:N0} rows move";
            _view.ShowPlan($"{Text.Count(FeederLayout.Buses(_saved).Count, "bus", "buses")}  ·  {state}", rows, _tagRows, moved > 0);
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
