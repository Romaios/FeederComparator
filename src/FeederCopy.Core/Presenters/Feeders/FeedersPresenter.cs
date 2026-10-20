using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Import;
using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Services.Data;
using FeederCopy.Core.Services.Import;
using FeederCopy.Core.Services.Logging;

namespace FeederCopy.Core.Presenters.Feeders
{
    public sealed class FeedersPresenter
    {
        private const string ListFilter = "Feeder lists (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|All files (*.*)|*.*";

        private readonly IFeedersView _view;
        private readonly IFeederService _feeders;
        private readonly Func<AppSettings> _settings;
        private readonly IErrorLog _log;
        private IReadOnlyList<FeederSummary> _all = [];
        private long? _selectedId;
        private PendingFeederList? _pending;

        public FeedersPresenter(IFeedersView view, IFeederService feeders, Func<AppSettings> settings, IErrorLog log)
        {
            _view = view;
            _feeders = feeders;
            _settings = settings;
            _log = log;
        }

        public event Action? FeedersChanged;

        private FeederSummary? Selected => _all.FirstOrDefault(f => f.Id == _selectedId);

        public void Load() => Run("Loading feeders", "Could not load the feeders.", Reload);

        public void OnFeederSelected(long feederId)
        {
            if (_selectedId == feederId)
                return;
            _selectedId = feederId;
            Run("Loading feeder consumers", "Could not load the feeder.", ShowSelected);
        }

        public Task OnImportNewClickedAsync() => ReadListAsync(PreviewMode.NewFeeder);

        public Task OnUpdateClickedAsync() => Selected is null ? Task.CompletedTask : ReadListAsync(PreviewMode.Update);

        public void OnPreviewNameChanged(string name)
        {
            if (_pending is null || _pending.Mode != PreviewMode.NewFeeder)
                return;
            _pending = _pending with { Name = name };
            _view.ShowPreview(_pending.ToPreview(_all));
        }

        public void OnSavePreviewClicked()
        {
            if (_pending is not { } pending || !pending.ToPreview(_all).CanSave)
                return;
            Change("Saving feeder list", "Could not save the feeder list.", () =>
            {
                if (pending.Mode == PreviewMode.NewFeeder)
                    _selectedId = _feeders.CreateFeeder(pending.Name.Trim(), pending.Parsed.Entries);
                else
                    _feeders.SaveConsumers(new Dictionary<long, IReadOnlyList<ConsumerEntry>> { [pending.FeederId!.Value] = pending.Parsed.Entries });
                _pending = null;
                _view.HidePreview();
            });
        }

        public void OnCancelPreviewClicked()
        {
            _pending = null;
            _view.HidePreview();
        }

        public void OnRenameClicked(string name)
        {
            if (Selected is not { } feeder)
                return;
            string? error = FeederRules.ValidateName(name, _all, feeder.Id);
            if (error is not null)
            {
                _view.ShowError(error);
                return;
            }
            Change("Renaming feeder", "Could not rename the feeder.", () => _feeders.RenameFeeder(feeder.Id, name.Trim()));
        }

        public void OnDeleteClicked()
        {
            if (Selected is not { } feeder)
                return;
            if (!_view.Confirm("Delete feeder", $"Delete feeder '{feeder.Name}' and its {feeder.Consumers} consumers?"))
                return;
            Change("Deleting feeder", "Could not delete the feeder.", () =>
            {
                _feeders.DeleteFeeder(feeder.Id);
                _selectedId = null;
            });
        }

        public void OnAddClicked(string? busId, string? consumerId)
        {
            if (Selected is not { } feeder)
                return;
            string? error = FeederRules.ValidateConsumerId(consumerId);
            if (error is not null)
            {
                _view.ShowAddError(error);
                return;
            }
            string id = FeederLayout.NormalizeConsumer(consumerId!);
            Change("Adding consumer", "Could not save the change.", () =>
            {
                if (_feeders.FindConsumer(id) is { } where)
                {
                    _view.ShowAddError(FeederRules.AlreadyIn(id, where));
                    return false;
                }
                Save(feeder.Id, FeederLayout.Add(_feeders.GetConsumers(feeder.Id), busId, id));
                _view.ClearAddForm();
                _view.ShowAddError(null);
                return true;
            }, id);
        }

        public void OnSetBusClicked(string consumerId, string? busId)
        {
            if (Selected is not { } feeder)
                return;
            Change("Setting bus ID", "Could not save the change.",
                () => Save(feeder.Id, FeederLayout.SetBus(_feeders.GetConsumers(feeder.Id), consumerId, busId)), consumerId);
        }

        public void OnMoveClicked(string consumerId, long targetFeederId)
        {
            if (Selected is not { } feeder || targetFeederId == feeder.Id)
                return;
            Change("Moving consumer", "Could not save the change.", () =>
            {
                IReadOnlyList<ConsumerEntry> source = _feeders.GetConsumers(feeder.Id);
                string? bus = source.FirstOrDefault(e => e.ConsumerId == consumerId)?.BusId;
                _feeders.SaveConsumers(new Dictionary<long, IReadOnlyList<ConsumerEntry>>
                {
                    [feeder.Id] = FeederLayout.Remove(source, consumerId),
                    [targetFeederId] = FeederLayout.Add(_feeders.GetConsumers(targetFeederId), bus, consumerId),
                });
            });
        }

        public void OnRemoveClicked(string consumerId)
        {
            if (Selected is not { } feeder)
                return;
            if (!_view.Confirm("Remove consumer", $"Remove consumer {consumerId} from {feeder.Name}?"))
                return;
            Change("Removing consumer", "Could not save the change.",
                () => Save(feeder.Id, FeederLayout.Remove(_feeders.GetConsumers(feeder.Id), consumerId)));
        }

        public void OnSearch(string? text)
        {
            string id = text?.Trim() ?? string.Empty;
            if (id.Length == 0)
            {
                _view.ShowSearchResult(null);
                return;
            }
            Run("Searching consumer", "Could not search.", () =>
            {
                if (_feeders.FindConsumer(id) is not { } where)
                {
                    _view.ShowSearchResult($"Consumer {id} isn't in any feeder.");
                    return;
                }
                _selectedId = where.FeederId;
                Reload();
                _view.SelectConsumer(id);
                _view.ShowSearchResult(null);
            });
        }

        private async Task ReadListAsync(PreviewMode mode)
        {
            string? path = _view.PickOpenFile(mode == PreviewMode.NewFeeder ? "Choose a feeder list" : "Choose the newer feeder list", ListFilter);
            if (path is null)
                return;

            FeederSummary? target = mode == PreviewMode.Update ? Selected : null;
            _view.SetBusy(true, "Reading feeder list…");
            try
            {
                FeederColumns columns = _settings().Feeder;
                (ParsedFeederList parsed, IReadOnlyList<string> conflicts, string summary) = await Task.Run(() => Inspect(path, columns, target));
                string name = target?.Name ?? Path.GetFileNameWithoutExtension(path);
                _pending = new PendingFeederList(mode, target?.Id, parsed, conflicts, summary, name);
                _view.SetBusy(false, null);
                _view.ShowPreview(_pending.ToPreview(_all));
            }
            catch (Exception ex)
            {
                _log.Error($"Reading feeder list {path}", ex);
                _view.SetBusy(false, null);
                _view.ShowError($"Could not read the feeder list. {ex.Message}");
            }
        }

        private (ParsedFeederList, IReadOnlyList<string>, string) Inspect(string path, FeederColumns columns, FeederSummary? target)
        {
            ParsedFeederList parsed = FeederListParser.Read(path, columns);
            IReadOnlyList<string> conflicts = _feeders.FindConsumers(parsed.Entries.Select(e => e.ConsumerId))
                .Where(f => f.Where.FeederId != target?.Id)
                .Select(f => FeederRules.AlreadyIn(f.ConsumerId, f.Where))
                .ToList();

            string consumers = PendingFeederList.Count(parsed.Entries.Count, "consumer", "consumers");
            string summary = target is null
                ? $"{consumers} · {PendingFeederList.Count(FeederLayout.Buses(parsed.Entries).Count, "bus", "buses")} · {PendingFeederList.Count(parsed.Warnings.Count, "row", "rows")} skipped"
                : $"{FeederDiff.Compare(_feeders.GetConsumers(target.Id), parsed.Entries)} · {consumers}";
            return (parsed, conflicts, summary);
        }

        private void Save(long feederId, IReadOnlyList<ConsumerEntry> entries) =>
            _feeders.SaveConsumers(new Dictionary<long, IReadOnlyList<ConsumerEntry>> { [feederId] = entries });

        private void Change(string context, string message, Action action, string? selectConsumer = null) =>
            Change(context, message, () => { action(); return true; }, selectConsumer);

        private void Change(string context, string message, Func<bool> action, string? selectConsumer = null)
        {
            bool changed = false;
            Run(context, message, () =>
            {
                changed = action();
                if (!changed)
                    return;
                Reload();
                if (selectConsumer is not null)
                    _view.SelectConsumer(selectConsumer);
            });
            if (changed)
                FeedersChanged?.Invoke();
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

        private void Reload()
        {
            _all = _feeders.GetFeeders();
            if (_all.Count == 0)
            {
                _selectedId = null;
                _view.ShowNoFeeders();
                return;
            }
            if (Selected is null)
                _selectedId = _all[0].Id;
            _view.ShowFeeders(_all, _selectedId);
            ShowSelected();
        }

        private void ShowSelected()
        {
            if (Selected is not { } feeder)
                return;
            IReadOnlyList<ConsumerEntry> entries = _feeders.GetConsumers(feeder.Id);
            _view.ShowConsumers(feeder, FeederLayout.ToRows(entries), FeederLayout.Buses(entries), _all.Where(f => f.Id != feeder.Id).ToList());
        }
    }
}
