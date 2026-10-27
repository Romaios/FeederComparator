using FeederCopy.Core.Models;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Services.Data;
using FeederCopy.Core.Services.Export;
using FeederCopy.Core.Services.Import;
using FeederCopy.Core.Services.Logging;

namespace FeederCopy.Core.Presenters.Copy
{
    public sealed class CopyPresenter
    {
        private const string AsBilledFilter = "As-Billed files (*.xls;*.xlsx;*.csv)|*.xls;*.xlsx;*.csv|All files (*.*)|*.*";
        private const string NoFeeders = "Import a feeder list in the Feeders tab first.";
        private const string ChooseFile = "Choose an As-Billed file to start.";
        private const string SaveFailed = "Could not save the file. If it is open in Excel, close it and try again.";
        public const string LoadingSaved = "Loading the saved kWh…";

        private readonly ICopyView _view;
        private readonly IFeederService _feeders;
        private readonly IReadingService _readings;
        private readonly IAsBilledReader _reader;
        private readonly IFeederExporter _exporter;
        private readonly Func<AppSettings> _settings;
        private readonly Action<ExportStyle> _rememberStyle;
        private readonly IErrorLog _log;
        private List<KwhSnapshot> _snapshots = [];
        private CopyResult? _result;
        private int _loadRequest;
        private Task _loading = Task.CompletedTask;

        public CopyPresenter(
            ICopyView view,
            IFeederService feeders,
            IReadingService readings,
            IAsBilledReader reader,
            IFeederExporter exporter,
            Func<AppSettings> settings,
            Action<ExportStyle> rememberStyle,
            IErrorLog log)
        {
            _view = view;
            _feeders = feeders;
            _readings = readings;
            _reader = reader;
            _exporter = exporter;
            _settings = settings;
            _rememberStyle = rememberStyle;
            _log = log;
        }

        public event Action? ReadingsSaved;

        public event Action? StatusesUpdated;

        public Task LoadAsync() => _loading = LoadSavedAsync();

        private async Task LoadSavedAsync()
        {
            int request = ++_loadRequest;
            _view.SetExportStyle(_settings().Export);
            _view.SetExportEnabled(false);
            _view.ClearResult(LoadingSaved);
            try
            {
                (BillMonth? latest, bool hasFeeders, CopyResult? saved) = await Task.Run(ReadLatestSaved);
                if (request != _loadRequest)
                    return;
                if (latest is { } month)
                {
                    _view.SetMonth(month.Month, month.Year);
                    ShowSaved(month, hasFeeders, saved);
                }
                else
                {
                    RefreshIdleState();
                }
            }
            catch (Exception ex)
            {
                if (request != _loadRequest)
                    return;
                _log.Error("Loading saved months", ex);
                _view.ClearResult(ChooseFile);
                _view.ShowError($"Could not load the saved kWh. {ex.Message}");
            }
        }

        private (BillMonth? Latest, bool HasFeeders, CopyResult? Saved) ReadLatestSaved()
        {
            IReadOnlyList<BillMonth> months = _readings.GetMonths();
            bool hasFeeders = _feeders.GetFeeders().Count > 0;
            if (months.Count == 0 || !hasFeeders)
                return (null, hasFeeders, null);
            BillMonth latest = months[^1];
            return (latest, true, ReadSaved(latest, true));
        }

        public void OnMonthChanged(int? month, int? year)
        {
            if (month is not (>= 1 and <= 12) || year is not (>= 1900 and <= 2100))
                return;
            var chosen = new BillMonth(year.Value, month.Value);
            if (_result?.Month == chosen && _result.SourceFile != KwhCopier.SavedSource)
                return;
            _loadRequest++;
            Run("Loading saved month", () => ShowSaved(chosen));
        }

        public async Task OnChooseFileClickedAsync()
        {
            IReadOnlyList<string> paths = _view.PickOpenFiles("Choose the As-Billed file (hold Ctrl to pick several months)", AsBilledFilter);
            if (paths.Count == 0)
                return;
            _loadRequest++;

            try
            {
                AsBilledColumns columns = _settings().AsBilled;
                var read = new List<KwhSnapshot>();
                var skipped = new List<string>();
                for (int i = 0; i < paths.Count; i++)
                {
                    string path = paths[i];
                    string name = Path.GetFileName(path);
                    string step = paths.Count == 1 ? "Reading" : $"Reading {name} ({i + 1} of {paths.Count})";
                    _view.SetBusy(true, $"{step}…");
                    var progress = new Progress<int>(rows => _view.SetBusy(true, $"{step}… {rows:N0} rows"));
                    KwhSnapshot snapshot = await Task.Run(() => _reader.Read(path, columns, progress));
                    if (snapshot.FatalError is not null)
                        skipped.Add(paths.Count == 1 ? snapshot.FatalError : $"{name}: {snapshot.FatalError}");
                    else if (paths.Count > 1 && snapshot.Month is null)
                        skipped.Add($"{name}: the bill month was not found in the file. Choose this file on its own and pick its month.");
                    else if (read.FirstOrDefault(r => r.Month == snapshot.Month) is { } same)
                        skipped.Add($"{name}: {snapshot.Month} is already in {same.FileName}.");
                    else
                        read.Add(snapshot);
                }
                _view.SetBusy(false, null);
                _result = null;
                _view.SetExportEnabled(false);
                _snapshots = read.OrderBy(r => r.Month).ToList();
                if (skipped.Count > 0)
                    _view.ShowError(paths.Count == 1 ? skipped[0] : "These files were skipped:\n" + string.Join("\n", skipped));
                ShowChosenFiles();
            }
            catch (Exception ex)
            {
                _log.Error($"Reading As-Billed files {string.Join(", ", paths)}", ex);
                _view.SetBusy(false, null);
                _view.ShowError($"Could not read the As-Billed file. {ex.Message}");
            }
        }

        public async Task OnCopyClickedAsync(int? month, int? year)
        {
            if (_snapshots.Count == 0)
                return;

            List<(KwhSnapshot Snapshot, BillMonth Month)> jobs;
            if (_snapshots.Count == 1)
            {
                if (month is not (>= 1 and <= 12) || year is not (>= 1900 and <= 2100))
                {
                    _view.ShowError("Choose the month and year.");
                    return;
                }
                KwhSnapshot snapshot = _snapshots[0];
                var chosen = new BillMonth(year.Value, month.Value);
                if (snapshot.Month is { } fileMonth && fileMonth != chosen
                    && !_view.Confirm("Different month", $"You chose {chosen}, but the file contains {fileMonth}. Use {chosen}?"))
                    return;
                jobs = [(snapshot, chosen)];
            }
            else
            {
                jobs = _snapshots.Select(snapshot => (snapshot, snapshot.Month!.Value)).ToList();
            }

            _loadRequest++;
            _view.SetBusy(true, "Transferring kWh…");
            try
            {
                bool negativeAsZero = _settings().NegativeKwhAsZero;
                CopyResult? last = null;
                bool lastUpdated = false;
                bool anyUpdated = false;
                BillMonth newest = jobs[^1].Month;
                for (int i = 0; i < jobs.Count; i++)
                {
                    (KwhSnapshot snapshot, BillMonth target) = jobs[i];
                    if (jobs.Count > 1)
                        _view.SetBusy(true, $"Transferring {target} ({i + 1} of {jobs.Count})…");
                    (CopyResult result, bool updated, BillMonth latest) = await Task.Run(() => CopyMonth(snapshot, target, negativeAsZero));
                    if (result.Feeders.Count == 0)
                    {
                        _view.SetBusy(false, null);
                        _view.ShowError(NoFeeders);
                        return;
                    }
                    last = result;
                    lastUpdated = updated;
                    anyUpdated |= updated;
                    newest = latest;
                }
                _view.SetBusy(false, null);

                ReadingsSaved?.Invoke();
                if (anyUpdated)
                    StatusesUpdated?.Invoke();
                string status = lastUpdated
                    ? "current status set from this month's bills"
                    : $"current status kept from {newest} (newer month)";
                BillMonth shown = last!.Month;
                string label = jobs.Count == 1
                    ? $"{shown} · transferred · {status}"
                    : $"{jobs.Count} months transferred ({jobs[0].Month} – {shown}) · showing {shown} · {status}";
                _view.SetMonth(shown.Month, shown.Year);
                _view.SetMonthEnabled(true);
                Show(last, label);
            }
            catch (Exception ex)
            {
                _log.Error("Transferring kWh", ex);
                _view.SetBusy(false, null);
                _view.ShowError($"Could not transfer the kWh. {ex.Message}");
            }
        }

        public void OnFeedersChanged()
        {
            if (!_loading.IsCompleted)
            {
                _ = LoadAsync();
                return;
            }
            if (_result is not { } result)
            {
                RefreshIdleState();
                return;
            }
            Run("Refreshing saved month", () =>
            {
                _view.SetCopyEnabled(HasFeeders() && _snapshots.Count > 0);
                ShowSaved(result.Month);
            });
        }

        public void OnSaveProblemsClicked()
        {
            if (_result is not { } result)
                return;
            string? path = _view.PickSaveFile("Text file|*.txt", $"kWh transfer problems - {result.Month}.txt");
            if (path is null)
                return;
            Save("Saving problem list", () => File.WriteAllText(path, CopyReport.ToText(result)));
        }

        public void OnExportClicked(ExportStyle style)
        {
            if (_result is not { } result)
                return;
            _rememberStyle(style);

            if (style == ExportStyle.OneWorkbook)
            {
                string? path = _view.PickSaveFile("Excel workbook|*.xlsx", ExportNames.WorkbookFileName(result.Month));
                if (path is null)
                    return;
                if (Save("Exporting workbook", () => _exporter.WriteWorkbook(path, result)))
                    _view.ShowInfo($"Exported {Text.Count(result.Feeders.Count, "feeder", "feeders")}.");
                return;
            }

            string? folder = _view.PickFolder();
            if (folder is null)
                return;
            int existing = _exporter.SeparatePaths(folder, result).Count(File.Exists);
            if (existing > 0)
            {
                string files = existing == 1 ? "1 file already exists" : $"{existing} files already exist";
                if (!_view.Confirm("Replace files", $"{files} in this folder and will be replaced. Continue?"))
                    return;
            }
            if (Save("Exporting separate files", () => _exporter.WriteSeparate(folder, result)))
                _view.ShowInfo($"Exported {Text.Count(result.Feeders.Count, "feeder", "feeders")}.");
        }

        private (CopyResult Result, bool Updated, BillMonth Latest) CopyMonth(KwhSnapshot snapshot, BillMonth month, bool negativeAsZero)
        {
            CopyResult filled = KwhCopier.Fill(LoadFeeders(), snapshot, month, negativeAsZero);
            if (filled.Feeders.Count == 0)
                return (filled, false, month);
            _readings.SaveMonth(month, KwhCopier.Readings(filled));
            BillMonth latest = _readings.GetMonths().Max();
            bool update = month >= latest;
            if (update)
                _feeders.ApplyMonthStatus(KwhCopier.Statuses(filled), month);
            return (filled, update, latest);
        }

        private void ShowChosenFiles()
        {
            _view.SetMonthEnabled(_snapshots.Count <= 1);
            RefreshIdleState();
            if (_snapshots.Count == 0)
            {
                _view.ShowFileText("No file chosen yet.");
                return;
            }

            int rows = _snapshots.Sum(snapshot => snapshot.RowsRead);
            int skippedRows = _snapshots.Sum(snapshot => snapshot.Errors.Count);
            IReadOnlyList<BillMonth> saved = _readings.GetMonths();
            if (_snapshots.Count == 1)
            {
                KwhSnapshot only = _snapshots[0];
                _view.ShowFileText($"{only.FileName}  ·  {only.Month?.ToString() ?? "month not found"}  ·  {rows:N0} rows  ·  {skippedRows:N0} skipped");
                _view.SetMonth(only.Month?.Month, only.Month?.Year);
                if (only.Month is { } fileMonth && saved.Contains(fileMonth))
                    _view.ClearResult($"{fileMonth} was transferred before. Click Transfer to transfer it again from this file.");
                return;
            }

            List<BillMonth> months = _snapshots.Select(snapshot => snapshot.Month!.Value).ToList();
            _view.ShowFileText($"{_snapshots.Count} files  ·  {months[0]} – {months[^1]}  ·  {rows:N0} rows  ·  {skippedRows:N0} skipped");
            _view.SetMonth(months[^1].Month, months[^1].Year);
            int again = months.Count(saved.Contains);
            string replaced = again == 0 ? string.Empty : $" {Text.Count(again, "month was", "months were")} transferred before and will be replaced.";
            _view.ClearResult($"Click Transfer to transfer {Text.Count(months.Count, "month", "months")}: {string.Join(", ", months)}.{replaced}");
        }

        private IReadOnlyList<FeederSource> LoadFeeders() =>
            _feeders.GetFeeders()
                .Select(f => new FeederSource(f.Name, _feeders.GetConsumers(f.Id)))
                .ToList();

        private CopyResult? ReadSaved(BillMonth month, bool hasFeeders)
        {
            if (!hasFeeders)
                return null;
            IReadOnlyList<ReadingRecord> saved = _readings.GetMonth(month);
            return saved.Count == 0 ? null : KwhCopier.FromReadings(LoadFeeders(), saved, month);
        }

        private void ShowSaved(BillMonth month)
        {
            bool hasFeeders = HasFeeders();
            ShowSaved(month, hasFeeders, ReadSaved(month, hasFeeders));
        }

        private void ShowSaved(BillMonth month, bool hasFeeders, CopyResult? saved)
        {
            if (saved is null)
            {
                _result = null;
                _view.SetExportEnabled(false);
                _view.ClearResult(!hasFeeders ? NoFeeders
                    : _snapshots.Count == 0 ? $"Nothing saved for {month} yet. Choose its As-Billed file, then click Transfer."
                    : "Check the month, then click Transfer.");
                return;
            }
            Show(saved, $"{month} · saved transfer");
        }

        private void Show(CopyResult result, string label)
        {
            _result = result;
            int inactive = result.Totals.Sum(t => t.Inactive);
            string heading = $"{label} · {Text.Count(result.Feeders.Count, "feeder", "feeders")} · {inactive:N0} inactive (no bill) · {Text.Count(result.Problems.Count, "problem", "problems")}";
            _view.ShowResult(result.Totals, result.Problems, heading);
            _view.SetExportEnabled(true);
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
                _view.ShowError($"Could not load the saved kWh. {ex.Message}");
            }
        }

        private bool HasFeeders()
        {
            try
            {
                return _feeders.GetFeeders().Count > 0;
            }
            catch (Exception ex)
            {
                _log.Error("Loading feeders", ex);
                _view.ShowError($"Could not load the feeders. {ex.Message}");
                return false;
            }
        }

        private void RefreshIdleState()
        {
            bool hasFeeders = HasFeeders();
            _view.SetCopyEnabled(hasFeeders && _snapshots.Count > 0);
            if (!hasFeeders)
                _view.ClearResult(NoFeeders);
            else if (_snapshots.Count == 0)
                _view.ClearResult(ChooseFile);
            else
                _view.ClearResult("Check the month, then click Transfer.");
        }

        private bool Save(string context, Action write)
        {
            try
            {
                write();
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _log.Error(context, ex);
                _view.ShowError(SaveFailed);
                return false;
            }
        }
    }
}
