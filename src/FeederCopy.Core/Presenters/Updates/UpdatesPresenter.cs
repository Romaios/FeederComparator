using FeederCopy.Core.Models.Updates;
using FeederCopy.Core.Services.Logging;
using FeederCopy.Core.Services.Updates;

namespace FeederCopy.Core.Presenters.Updates
{
    public sealed class UpdatesPresenter
    {
        private readonly IUpdatesView _view;
        private readonly IUpdateService _service;
        private readonly IUpdateSettingsStore _store;
        private readonly IErrorLog _log;
        private readonly Func<DateTime> _clock;
        private readonly Func<TimeSpan, Task> _delay;
        private UpdateSettings _settings = UpdateSettings.Empty;
        private UpdateOffer? _offer;
        private CancellationTokenSource? _download;
        private UpdateStage _stage = UpdateStage.Idle;
        private DateTime _stageStartedAt;
        private DateTime _lastProgressAt;
        private DateTime _downloadStartedAt;
        private int _lastPercent;

        public UpdatesPresenter(IUpdatesView view, IUpdateService service, IUpdateSettingsStore store, IErrorLog log, Func<DateTime>? clock = null, Func<TimeSpan, Task>? delay = null)
        {
            _view = view;
            _service = service;
            _store = store;
            _log = log;
            _clock = clock ?? (() => DateTime.UtcNow);
            _delay = delay ?? Task.Delay;
        }

        public const int StallWarningSeconds = 20;
        private static readonly TimeSpan FullBarPause = TimeSpan.FromSeconds(1);
        private const string DownloadingMessage = "Keep the program open. It will close and reopen by itself when the update is ready.";

        public event Action<string>? UpdateAvailable;

        public void Load()
        {
            try
            {
                _settings = _store.Load();
            }
            catch (Exception ex)
            {
                _log.Error("Reading the update settings", ex);
                _settings = UpdateSettings.Empty;
                _view.ShowSourceMessage("The saved update settings could not be read. Enter the access token again and save.", isError: true);
            }
            _view.ShowSource(_settings.Repository, _settings.Token.Length > 0, _service.CurrentVersion);
            ShowStage(InitialStage());
        }

        public void OnSaveSourceClicked(string repository, string token)
        {
            var updated = new UpdateSettings(repository.Trim(), token.Trim().Length > 0 ? token.Trim() : _settings.Token);
            if (updated.RepositoryUrl is null)
            {
                _view.ShowSourceMessage("Enter the repository as owner/name, for example Romaios/FeederComparator.", isError: true);
                return;
            }
            if (updated.Token.Length == 0)
            {
                _view.ShowSourceMessage("Enter the access token.", isError: true);
                return;
            }

            try
            {
                _store.Save(updated);
            }
            catch (Exception ex)
            {
                _log.Error("Saving the update settings", ex);
                _view.ShowSourceMessage($"Could not save the update settings: {ex.Message}", isError: true);
                return;
            }

            _settings = updated;
            _view.ShowSource(_settings.Repository, hasToken: true, _service.CurrentVersion);
            _view.ShowSourceMessage("Saved.", isError: false);
            ShowStage(InitialStage());
        }

        public async Task OnCheckClickedAsync()
        {
            UpdateStage stage = InitialStage();
            if (stage != UpdateStage.Idle)
            {
                ShowStage(stage);
                return;
            }

            ShowStage(UpdateStage.Checking);
            try
            {
                _offer = await _service.CheckAsync(_settings, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _log.Error("Checking for updates", ex);
                ShowFailure("Could not check for updates", ex);
                return;
            }
            ShowStage(_offer is null ? UpdateStage.UpToDate : UpdateStage.Available);
        }

        public async Task CheckQuietlyAsync()
        {
            if (InitialStage() != UpdateStage.Idle)
                return;
            try
            {
                _offer = await _service.CheckAsync(_settings, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _log.Error("Checking for updates at start-up", ex);
                ShowFailure("Could not check for updates", ex);
                return;
            }
            if (_offer is null)
                return;
            ShowStage(UpdateStage.Available);
            UpdateAvailable?.Invoke(_offer.Version);
        }

        public async Task OnUpdateNowClickedAsync()
        {
            if (_offer is not UpdateOffer offer || _download is not null)
                return;

            ShowStage(UpdateStage.Downloading);
            _lastPercent = 0;
            _lastProgressAt = _clock();
            _downloadStartedAt = _lastProgressAt;
            _view.ShowProgress(0, ProgressText(0, offer.SizeBytes, 0));
            var progress = new Progress<int>(percent => OnDownloadProgress(percent, offer.SizeBytes));

            using var cancel = new CancellationTokenSource();
            _download = cancel;
            try
            {
                await _service.DownloadAsync(offer, progress, cancel.Token);
            }
            catch (OperationCanceledException) when (cancel.IsCancellationRequested)
            {
                ShowStage(UpdateStage.Available, "Download cancelled. Click Update now to try again.");
                return;
            }
            catch (Exception ex)
            {
                _log.Error($"Downloading version {offer.Version}", ex);
                ShowFailure("Download failed", ex);
                return;
            }
            finally
            {
                _download = null;
            }

            ShowStage(UpdateStage.Applying);
            _view.ShowProgress(100, $"Downloaded {Megabytes(offer.SizeBytes)}. Installing…");
            await _delay(FullBarPause);
            try
            {
                _service.ApplyAndRestart(offer);
            }
            catch (Exception ex)
            {
                _log.Error($"Installing version {offer.Version}", ex);
                ShowFailure("Could not install the update", ex);
            }
        }

        public void OnDownloadProgress(int percent, long totalBytes)
        {
            if (_stage != UpdateStage.Downloading)
                return;
            int clamped = Math.Clamp(percent, 0, 100);
            if (clamped != _lastPercent)
            {
                _lastPercent = clamped;
                _lastProgressAt = _clock();
                _view.ShowMessage(DownloadingMessage);
            }
            _view.ShowProgress(clamped, ProgressText(clamped, totalBytes, (_clock() - _downloadStartedAt).TotalSeconds));
        }

        public void OnTimerTick()
        {
            DateTime now = _clock();
            if (_stage == UpdateStage.Checking)
            {
                _view.ShowDetail($"Waiting for GitHub… {(int)(now - _stageStartedAt).TotalSeconds} s");
                return;
            }
            if (_stage != UpdateStage.Downloading)
                return;
            int idle = (int)(now - _lastProgressAt).TotalSeconds;
            _view.ShowMessage(idle >= StallWarningSeconds
                ? $"The connection is slow. No data received for {idle} seconds, still trying…"
                : DownloadingMessage);
        }

        public void OnCancelClicked()
        {
            if (_download is not CancellationTokenSource download || _stage != UpdateStage.Downloading)
                return;
            ShowStage(UpdateStage.Cancelling);
            download.Cancel();
        }

        public bool OnAppClosing(bool userClosing)
        {
            if (!userClosing)
                return false;
            if (_stage == UpdateStage.Downloading)
                OnCancelClicked();
            return _stage is UpdateStage.Downloading or UpdateStage.Cancelling or UpdateStage.Applying;
        }

        private UpdateStage InitialStage() =>
            !_service.IsInstalled ? UpdateStage.NotInstalled
            : !_settings.IsConfigured ? UpdateStage.NeedsSettings
            : UpdateStage.Idle;

        private void ShowFailure(string title, Exception ex)
        {
            _stage = UpdateStage.Failed;
            _view.ShowScreen(new UpdateScreen(UpdateStage.Failed, title, ex.Message, CanCheck: true, CanUpdate: false, CanCancel: false, ShowProgress: false));
        }

        private void ShowStage(UpdateStage stage, string? message = null)
        {
            _stage = stage;
            _stageStartedAt = _clock();
            string version = _offer?.Version ?? string.Empty;
            UpdateScreen screen = stage switch
            {
                UpdateStage.NotInstalled => new(stage, "Updates are not available in this copy",
                    "This copy runs from a plain folder. Install the program with FeederCopy-Setup.exe from the GitHub release to get automatic updates.",
                    false, false, false, false),
                UpdateStage.NeedsSettings => new(stage, "Set up the update source",
                    "Enter the repository and the access token above, then click Save.", false, false, false, false),
                UpdateStage.Checking => new(stage, "Checking for updates", "Contacting GitHub. This can take up to a minute.", false, false, false, true),
                UpdateStage.UpToDate => new(stage, "You're up to date", $"You are running the latest version ({_service.CurrentVersion}).", true, false, false, false),
                UpdateStage.Available => new(stage, $"Version {version} is available",
                    message ?? $"You have version {_service.CurrentVersion}. The download is {Megabytes(_offer?.SizeBytes ?? 0)}. The program will close and reopen by itself when the update is installed.",
                    true, true, false, false),
                UpdateStage.Downloading => new(stage, $"Downloading version {version}",
                    DownloadingMessage, false, false, true, true),
                UpdateStage.Cancelling => new(stage, "Cancelling download", "Stopping the download…", false, false, false, true),
                UpdateStage.Applying => new(stage, "Installing update",
                    $"The program will close now and reopen on version {version} by itself. This can take a minute or two; don't start it again in the meantime.", false, false, false, true),
                _ => new(stage, "Program updates", "Click Check for updates to look for a newer version.", true, false, false, false),
            };
            _view.ShowScreen(screen);
        }

        private static string ProgressText(int percent, long totalBytes, double seconds)
        {
            long received = totalBytes * percent / 100;
            string text = $"{Megabytes(received)} of {Megabytes(totalBytes)} ({percent}%)";
            return percent > 0 && seconds >= 1 ? $"{text} · {received / 1048576d / seconds:0.0} MB/s" : text;
        }

        private static string Megabytes(long bytes) => $"{bytes / 1048576d:0.0} MB";
    }
}
