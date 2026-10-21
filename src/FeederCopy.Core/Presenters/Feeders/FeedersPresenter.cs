using FeederCopy.Core.Models;
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
        private const string ListFilter = "Feeder workbooks (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|All files (*.*)|*.*";

        private readonly IFeedersView _view;
        private readonly IFeederService _feeders;
        private readonly Func<AppSettings> _settings;
        private readonly IErrorLog _log;
        private IReadOnlyList<FeederSummary> _all = [];
        private long? _selectedId;
        private ParsedWorkbook? _pending;
        private ImportPreview? _pendingPreview;

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

        public void OnCreateClicked(string? name)
        {
            string? error = FeederRules.ValidateName(name, _all, null);
            if (error is not null)
            {
                _view.ShowCreateError(error);
                return;
            }
            Change("Creating feeder", "Could not create the feeder.", () =>
            {
                _selectedId = _feeders.CreateFeeder(name!.Trim(), []);
                _view.ClearCreateForm();
                _view.ShowCreateError(null);
            });
        }

        public async Task OnImportClickedAsync()
        {
            string? path = _view.PickOpenFile("Choose the feeder workbook", ListFilter);
            if (path is null)
                return;

            _view.SetBusy(true, "Reading feeder workbook…");
            try
            {
                FeederColumns columns = _settings().Feeder;
                IReadOnlyList<FeederSummary> existing = _all;
                (ParsedWorkbook parsed, ImportPreview preview) = await Task.Run(() => Inspect(path, columns, existing));
                _pending = parsed;
                _pendingPreview = preview;
                _view.SetBusy(false, null);
                _view.ShowPreview(preview);
            }
            catch (Exception ex)
            {
                _log.Error($"Reading feeder workbook {path}", ex);
                _view.SetBusy(false, null);
                _view.ShowError($"Could not read the feeder workbook. {ex.Message}");
            }
        }

        public void OnSavePreviewClicked()
        {
            if (_pending is not { } parsed || _pendingPreview is not { CanSave: true })
                return;
            Change("Saving feeder workbook", "Could not save the feeders.", () =>
            {
                IReadOnlyList<long> ids = _feeders.ImportFeeders(parsed.Feeders.Select(f => (f.Name, f.Entries)).ToList());
                _selectedId = ids[0];
                _pending = null;
                _pendingPreview = null;
                _view.HidePreview();
            });
        }

        public void OnCancelPreviewClicked()
        {
            _pending = null;
            _pendingPreview = null;
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

        public void OnSortClicked()
        {
            if (Selected is not { } feeder)
                return;
            IReadOnlyList<string> tags = BusSorter.ParseTags(_settings().HandPlacedBusTags);
            IReadOnlyList<ConsumerEntry> current = FeederLayout.Arrange(_feeders.GetConsumers(feeder.Id));
            List<ConsumerEntry> sorted = BusSorter.Sort(current, tags);
            int moved = BusSorter.MovedRows(current, sorted);
            if (moved == 0)
            {
                _view.ShowInfo($"The bus IDs in {feeder.Name} are already in order.");
                return;
            }
            string tagText = tags.Count == 0 ? string.Empty : $" Buses with {string.Join("/", tags)} stay under their parent bus in the order you placed them.";
            if (!_view.Confirm("Sort bus IDs", $"Sort the bus IDs in {feeder.Name}? {moved:N0} of {current.Count:N0} rows will move.{tagText}"))
                return;
            Change("Sorting bus IDs", "Could not save the sorted list.", () => Save(feeder.Id, sorted));
        }

        public void OnAddClicked(string? busId, string? consumerId, string? placeAfterBus = null)
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
                IReadOnlyList<ConsumerEntry> entries = _feeders.GetConsumers(feeder.Id);
                string? bus = FeederLayout.NormalizeBus(busId);
                bool isNewBus = bus is not null && !entries.Any(e => string.Equals(e.BusId, bus, StringComparison.OrdinalIgnoreCase));
                IReadOnlyList<string> tags = BusSorter.ParseTags(_settings().HandPlacedBusTags);
                if (isNewBus && string.IsNullOrWhiteSpace(placeAfterBus) && BusSorter.IsHandPlaced(bus!, tags))
                {
                    _view.ShowAddError($"{bus} has {string.Join("/", tags)}. Choose where it goes in 'Place new bus after'.");
                    return false;
                }
                Save(feeder.Id, string.IsNullOrWhiteSpace(placeAfterBus)
                    ? FeederLayout.Add(entries, bus, id)
                    : FeederLayout.AddAfter(entries, new ConsumerEntry(bus, id), placeAfterBus));
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
                ConsumerEntry moving = source.FirstOrDefault(e => e.ConsumerId == consumerId) ?? new ConsumerEntry(null, consumerId);
                _feeders.SaveConsumers(new Dictionary<long, IReadOnlyList<ConsumerEntry>>
                {
                    [feeder.Id] = FeederLayout.Remove(source, consumerId),
                    [targetFeederId] = FeederLayout.Add(_feeders.GetConsumers(targetFeederId), moving),
                });
            });
        }

        public void OnSetActiveClicked(string consumerId, bool active)
        {
            if (Selected is null)
                return;
            Change("Setting consumer status", "Could not save the change.", () => _feeders.SetActive(consumerId, active), consumerId);
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

        private (ParsedWorkbook, ImportPreview) Inspect(string path, FeederColumns columns, IReadOnlyList<FeederSummary> existing)
        {
            ParsedWorkbook parsed = FeederListParser.ReadWorkbook(path, columns);
            var imported = parsed.Feeders.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            List<string> conflicts = _feeders.FindConsumers(parsed.Feeders.SelectMany(f => f.Entries).Select(e => e.ConsumerId))
                .Where(f => !imported.Contains(f.Where.FeederName))
                .Select(f => FeederRules.AlreadyIn(f.ConsumerId, f.Where))
                .ToList();

            var lines = new List<string>();
            foreach (ParsedSheet sheet in parsed.Feeders)
            {
                int inactive = sheet.Entries.Count(e => !e.Active);
                string consumers = Text.Count(sheet.Entries.Count, "consumer", "consumers") + (inactive > 0 ? $" ({inactive:N0} inactive)" : string.Empty);
                FeederSummary? saved = existing.FirstOrDefault(f => string.Equals(f.Name, sheet.Name, StringComparison.OrdinalIgnoreCase));
                lines.Add(saved is null
                    ? $"{sheet.Name} — new feeder · {consumers} · {Text.Count(FeederLayout.Buses(sheet.Entries).Count, "bus", "buses")}"
                    : $"{saved.Name} — replaces the saved list · {FeederDiff.Compare(_feeders.GetConsumers(saved.Id), sheet.Entries)} · {consumers}");
            }

            List<string> problems = [.. parsed.Problems, .. conflicts];
            var preview = new ImportPreview(
                $"Import from {Path.GetFileName(path)}",
                lines,
                problems,
                parsed.Notes,
                problems.Count == 0 && parsed.Feeders.Count > 0);
            return (parsed, preview);
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

