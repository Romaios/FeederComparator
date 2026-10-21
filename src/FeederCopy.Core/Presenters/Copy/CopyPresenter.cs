using FeederCopy.Core.Models;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Consumers;
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

        private readonly ICopyView _view;
        private readonly IFeederService _feeders;
        private readonly IReadingService _readings;
        private readonly IAsBilledReader _reader;
        private readonly IFeederExporter _exporter;
        private readonly Func<AppSettings> _settings;
        private readonly Action<ExportStyle> _rememberStyle;
        private readonly IErrorLog _log;
        private KwhSnapshot? _snapshot;
        private CopyResult? _result;

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

        public void Load()
        {
            _view.SetExportStyle(_settings().Export);
            _view.SetExportEnabled(false);
            RefreshIdleState();
        }

        public async Task OnChooseFileClickedAsync()
        {
            string? path = _view.PickOpenFile("Choose the As-Billed file", AsBilledFilter);
            if (path is null)
                return;

            _view.SetBusy(true, "Reading As-Billed file…");
            try
            {
                AsBilledColumns columns = _settings().AsBilled;
                var progress = new Progress<int>(rows => _view.SetBusy(true, $"Reading… {rows:N0} rows"));
                KwhSnapshot snapshot = await Task.Run(() => _reader.Read(path, columns, progress));
                _view.SetBusy(false, null);
                _result = null;
                _view.SetExportEnabled(false);

                if (snapshot.FatalError is not null)
                {
                    _snapshot = null;
                    _view.ShowError(snapshot.FatalError);
                    RefreshIdleState();
                    return;
                }

                _snapshot = snapshot;
                _view.ShowFileRead(snapshot.FileName, snapshot.Month, snapshot.RowsRead, snapshot.Errors.Count);
                _view.SetMonth(snapshot.Month?.Month, snapshot.Month?.Year);
                RefreshIdleState();
            }
            catch (Exception ex)
            {
                _log.Error($"Reading As-Billed file {path}", ex);
                _view.SetBusy(false, null);
                _view.ShowError($"Could not read the As-Billed file. {ex.Message}");
            }
        }

        public async Task OnCopyClickedAsync(int? month, int? year)
        {
            if (_snapshot is not { } snapshot)
                return;
            if (month is not (>= 1 and <= 12) || year is not (>= 1900 and <= 2100))
            {
                _view.ShowError("Choose the month and year.");
                return;
            }

            var chosen = new BillMonth(year.Value, month.Value);
            if (snapshot.Month is { } fileMonth && fileMonth != chosen
                && !_view.Confirm("Different month", $"You chose {chosen}, but the file contains {fileMonth}. Use {chosen}?"))
                return;

            _view.SetBusy(true, "Copying kWh…");
            try
            {
                CopyResult result = await Task.Run(() =>
                {
                    CopyResult filled = KwhCopier.Fill(LoadFeeders(chosen), snapshot, chosen);
                    if (filled.Feeders.Count > 0)
                        _readings.SaveMonth(chosen, KwhCopier.Readings(filled));
                    return filled;
                });
                _view.SetBusy(false, null);
                if (result.Feeders.Count == 0)
                {
                    _view.ShowError(NoFeeders);
                    return;
                }

                _result = result;
                ReadingsSaved?.Invoke();
                string heading = $"{chosen} · {Text.Count(result.Feeders.Count, "feeder", "feeders")} · {Text.Count(result.Problems.Count, "problem", "problems")}";
                _view.ShowResult(result.Totals, result.Problems, heading);
                _view.SetExportEnabled(true);
            }
            catch (Exception ex)
            {
                _log.Error("Copying kWh", ex);
                _view.SetBusy(false, null);
                _view.ShowError($"Could not copy the kWh. {ex.Message}");
            }
        }

        public void OnFeedersChanged()
        {
            if (_result is null)
            {
                RefreshIdleState();
                return;
            }
            _result = null;
            _view.SetExportEnabled(false);
            _view.ClearResult("Feeders changed. Click Copy kWh again.");
            _view.SetCopyEnabled(HasFeeders());
        }

        public void OnStatusChanged()
        {
            if (_result is not null)
                OnFeedersChanged();
        }

        public void OnSaveProblemsClicked()
        {
            if (_result is not { } result)
                return;
            string? path = _view.PickSaveFile("Text file|*.txt", $"kWh copy problems - {result.Month}.txt");
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

        private IReadOnlyList<FeederSource> LoadFeeders(BillMonth month)
        {
            IReadOnlyDictionary<string, BillMonth> inactiveFrom = _feeders.GetInactiveFrom();
            return _feeders.GetFeeders()
                .Select(f => new FeederSource(f.Name, _feeders.GetConsumers(f.Id)
                    .Select(e => !e.Active && inactiveFrom.TryGetValue(e.ConsumerId, out BillMonth from) && InactiveMonths.IsActiveIn(false, from, month)
                        ? e with { Active = true }
                        : e)
                    .ToList()))
                .ToList();
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
            _view.SetCopyEnabled(hasFeeders && _snapshot is not null);
            if (!hasFeeders)
                _view.ClearResult(NoFeeders);
            else if (_snapshot is null)
                _view.ClearResult(ChooseFile);
            else
                _view.ClearResult("Check the month, then click Copy kWh.");
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
