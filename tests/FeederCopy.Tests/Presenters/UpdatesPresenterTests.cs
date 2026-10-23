using FeederCopy.Core.Models.Updates;
using FeederCopy.Core.Presenters.Updates;

namespace FeederCopy.Tests.Presenters
{
    public class UpdatesPresenterTests
    {
        private static readonly UpdateSettings Configured = new("Romaios/FeederComparator", "secret");
        private static readonly UpdateOffer Offer = new("2.4.0", 52428800, new object());
        private readonly FakeUpdatesView _view = new();
        private readonly FakeUpdateService _service = new();
        private readonly FakeUpdateSettingsStore _store = new();
        private readonly FakeErrorLog _log = new();
        private readonly UpdatesPresenter _presenter;
        private DateTime _now = new(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc);

        public UpdatesPresenterTests() => _presenter = new UpdatesPresenter(_view, _service, _store, _log, () => _now);

        [Fact]
        public void A_plain_folder_copy_says_updates_need_the_installer()
        {
            _service.IsInstalled = false;
            _store.Stored = Configured;

            _presenter.Load();

            Assert.Equal(UpdateStage.NotInstalled, _view.Screen.Stage);
            Assert.False(_view.Screen.CanCheck);
        }

        [Fact]
        public void Without_a_token_it_asks_for_the_update_source()
        {
            _presenter.Load();

            Assert.Equal(("Romaios/FeederComparator", false, "2.3.0"), _view.Source);
            Assert.Equal(UpdateStage.NeedsSettings, _view.Screen.Stage);
        }

        [Fact]
        public void Unreadable_saved_settings_are_logged_and_reported()
        {
            _store.LoadError = new InvalidDataException("bad file");

            _presenter.Load();

            Assert.Equal(["Reading the update settings"], _log.Contexts);
            Assert.True(_view.SourceMessage?.IsError);
            Assert.Equal(UpdateStage.NeedsSettings, _view.Screen.Stage);
        }

        [Fact]
        public void Saving_rejects_a_repository_that_is_not_owner_and_name()
        {
            _presenter.Load();

            _presenter.OnSaveSourceClicked("FeederCopy", "secret");

            Assert.Null(_store.Saved);
            Assert.True(_view.SourceMessage?.IsError);
        }

        [Fact]
        public void Saving_with_an_empty_token_box_keeps_the_saved_token()
        {
            _store.Stored = Configured;
            _presenter.Load();

            _presenter.OnSaveSourceClicked(" Romaios/Other ", "  ");

            Assert.Equal(new UpdateSettings("Romaios/Other", "secret"), _store.Saved);
            Assert.Equal(("Saved.", false), _view.SourceMessage);
            Assert.Equal(UpdateStage.Idle, _view.Screen.Stage);
        }

        [Fact]
        public void Saving_needs_a_token_the_first_time()
        {
            _presenter.Load();

            _presenter.OnSaveSourceClicked("Romaios/FeederComparator", "");

            Assert.Null(_store.Saved);
            Assert.Equal(("Enter the access token.", true), _view.SourceMessage);
        }

        [Fact]
        public async Task Check_reports_up_to_date_when_there_is_no_newer_version()
        {
            _store.Stored = Configured;
            _presenter.Load();

            await _presenter.OnCheckClickedAsync();

            Assert.Equal(Configured, _service.CheckedWith);
            Assert.Equal([UpdateStage.Idle, UpdateStage.Checking, UpdateStage.UpToDate], _view.Screens.Select(s => s.Stage));
        }

        [Fact]
        public async Task Check_offers_the_newer_version_with_its_size()
        {
            _store.Stored = Configured;
            _service.Offer = Offer;
            _presenter.Load();

            await _presenter.OnCheckClickedAsync();

            Assert.Equal(UpdateStage.Available, _view.Screen.Stage);
            Assert.Equal("Version 2.4.0 is available", _view.Screen.Title);
            Assert.Contains($"{50.0:0.0} MB", _view.Screen.Message);
            Assert.True(_view.Screen.CanUpdate);
        }

        [Fact]
        public async Task A_failed_check_is_logged_and_shown()
        {
            _store.Stored = Configured;
            _service.CheckError = new HttpRequestException("Not Found");
            _presenter.Load();

            await _presenter.OnCheckClickedAsync();

            Assert.Equal(["Checking for updates"], _log.Contexts);
            Assert.Equal((UpdateStage.Failed, "Not Found"), (_view.Screen.Stage, _view.Screen.Message));
        }

        [Fact]
        public async Task Quiet_check_raises_the_banner_only_when_a_newer_version_exists()
        {
            _store.Stored = Configured;
            _presenter.Load();
            var announced = new List<string>();
            _presenter.UpdateAvailable += announced.Add;

            await _presenter.CheckQuietlyAsync();
            _service.Offer = Offer;
            await _presenter.CheckQuietlyAsync();

            Assert.Equal(["2.4.0"], announced);
        }

        [Fact]
        public async Task Quiet_check_does_nothing_until_the_source_is_set()
        {
            _presenter.Load();

            await _presenter.CheckQuietlyAsync();

            Assert.Equal(0, _service.Checks);
        }

        [Fact]
        public async Task Update_now_downloads_then_installs_and_restarts()
        {
            _store.Stored = Configured;
            _service.Offer = Offer;
            _presenter.Load();
            await _presenter.OnCheckClickedAsync();

            await _presenter.OnUpdateNowClickedAsync();

            Assert.Same(Offer, _service.Applied);
            Assert.Equal((0, $"{0.0:0.0} MB of {50.0:0.0} MB (0%)"), _view.Progress[0]);
            Assert.Equal([UpdateStage.Downloading, UpdateStage.Applying], _view.Screens.Skip(3).Select(s => s.Stage));
        }

        [Fact]
        public async Task Cancelling_the_download_offers_the_update_again()
        {
            _store.Stored = Configured;
            _service.Offer = Offer;
            _service.DuringDownload = _presenter.OnCancelClicked;
            _presenter.Load();
            await _presenter.OnCheckClickedAsync();

            await _presenter.OnUpdateNowClickedAsync();

            Assert.Null(_service.Applied);
            Assert.Contains(UpdateStage.Cancelling, _view.Screens.Select(s => s.Stage));
            Assert.Equal(UpdateStage.Available, _view.Screen.Stage);
            Assert.StartsWith("Download cancelled", _view.Screen.Message);
        }

        [Fact]
        public async Task Checking_shows_how_long_it_has_been_waiting()
        {
            _store.Stored = Configured;
            _presenter.Load();
            _service.DuringCheck = () =>
            {
                _now = _now.AddSeconds(7);
                _presenter.OnTimerTick();
            };

            await _presenter.OnCheckClickedAsync();

            Assert.Equal("Waiting for GitHub… 7 s", _view.Detail);
        }

        [Fact]
        public async Task A_stalled_download_warns_that_the_connection_is_slow()
        {
            _store.Stored = Configured;
            _service.Offer = Offer;
            _presenter.Load();
            await _presenter.OnCheckClickedAsync();
            _service.DuringDownload = () =>
            {
                _now = _now.AddSeconds(5);
                _presenter.OnDownloadProgress(10, Offer.SizeBytes);
                _now = _now.AddSeconds(UpdatesPresenter.StallWarningSeconds - 1);
                _presenter.OnTimerTick();
                Assert.StartsWith("Keep the program open", _view.Message);
                _now = _now.AddSeconds(6);
                _presenter.OnTimerTick();
            };

            await _presenter.OnUpdateNowClickedAsync();

            Assert.Equal("The connection is slow. No data received for 25 seconds, still trying…", _view.Message);
            Assert.Contains((10, $"{5.0:0.0} MB of {50.0:0.0} MB (10%) · {1.0:0.0} MB/s"), _view.Progress);
        }

        [Fact]
        public async Task Closing_the_program_during_a_download_cancels_it_and_stays_open()
        {
            _store.Stored = Configured;
            _service.Offer = Offer;
            _presenter.Load();
            await _presenter.OnCheckClickedAsync();
            bool? blocked = null;
            _service.DuringDownload = () => blocked = _presenter.OnAppClosing(userClosing: true);

            await _presenter.OnUpdateNowClickedAsync();

            Assert.True(blocked);
            Assert.Null(_service.Applied);
            Assert.False(_presenter.OnAppClosing(userClosing: true));
        }

        [Fact]
        public void Closing_is_allowed_when_nothing_is_downloading()
        {
            _store.Stored = Configured;
            _presenter.Load();

            Assert.False(_presenter.OnAppClosing(userClosing: true));
        }

        [Fact]
        public async Task A_failed_download_is_logged_and_does_not_install()
        {
            _store.Stored = Configured;
            _service.Offer = Offer;
            _service.DownloadError = new IOException("disk full");
            _presenter.Load();
            await _presenter.OnCheckClickedAsync();

            await _presenter.OnUpdateNowClickedAsync();

            Assert.Null(_service.Applied);
            Assert.Equal(["Downloading version 2.4.0"], _log.Contexts);
            Assert.Equal(("Download failed", "disk full"), (_view.Screen.Title, _view.Screen.Message));
        }
    }
}
