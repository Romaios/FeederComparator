using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Presenters.Settings;

namespace FeederCopy.Tests.Presenters
{
    public sealed class SettingsPresenterTests
    {
        private readonly FakeSettingsView _view = new();
        private readonly FakeSettingsStore _store = new();
        private readonly SettingsPresenter _presenter;

        public SettingsPresenterTests() => _presenter = new SettingsPresenter(_view, _store);

        private static AppSettings Custom => AppSettings.Defaults with { Feeder = new FeederColumns { BusId = "BUS", ConsumerId = "ACCT" } };

        [Fact]
        public void Load_ShowsSettingsAndReturnsProblem()
        {
            _store.LoadResult = new SettingsLoadResult(Custom, "bad");
            Assert.Equal("bad", _presenter.Load());
            Assert.Equal(Custom, _view.Shown);
            Assert.Equal(Custom, _presenter.Current);
        }

        [Fact]
        public void Save_Invalid_ShowsErrorsDoesNotSave()
        {
            _presenter.Load();
            _view.Entered = AppSettings.Defaults with { Feeder = FeederColumns.Defaults with { BusId = "" } };
            _presenter.OnSaveClicked();
            Assert.Contains("Enter the feeder list column name for Bus ID.", _view.Validation!);
            Assert.Empty(_store.Saved);
            Assert.Equal(AppSettings.Defaults, _presenter.Current);
        }

        [Fact]
        public void Save_Valid_SavesRaisesChanged()
        {
            _presenter.Load();
            int changed = 0;
            _presenter.SettingsChanged += () => changed++;
            _view.Entered = Custom;
            _presenter.OnSaveClicked();
            Assert.Equal([Custom], _store.Saved);
            Assert.Equal(Custom, _presenter.Current);
            Assert.Equal(1, changed);
            Assert.Equal(1, _view.SavedCount);
            Assert.Empty(_view.Validation!);
        }

        [Fact]
        public void Save_ThemeChanged_ConfirmYes_Restarts()
        {
            _presenter.Load();
            _view.Entered = AppSettings.Defaults with { Theme = ThemeMode.Dark };
            _presenter.OnSaveClicked();
            Assert.Equal(["Dark mode is saved. Restart the program now to switch?\n\nChoose No to switch the next time you open the program."], _view.Confirms);
            Assert.True(_view.Restarted);
            Assert.Equal(ThemeMode.Dark, _store.Saved.Single().Theme);
        }

        [Fact]
        public void Save_ThemeChanged_ConfirmNo_SavesWithoutRestart()
        {
            _presenter.Load();
            _view.ConfirmAnswer = false;
            _view.Entered = AppSettings.Defaults with { Theme = ThemeMode.Dark };
            _presenter.OnSaveClicked();
            Assert.False(_view.Restarted);
            Assert.Equal(ThemeMode.Dark, _presenter.Current.Theme);
        }

        [Fact]
        public void Save_ThemeUnchanged_NoPrompt()
        {
            _presenter.Load();
            _view.Entered = Custom;
            _presenter.OnSaveClicked();
            Assert.Empty(_view.Confirms);
            Assert.False(_view.Restarted);
        }

        [Fact]
        public void RememberHandPlacedTags_SavesOnlyWhenDifferent()
        {
            var presenter = new SettingsPresenter(_view, _store);
            presenter.Load();
            presenter.RememberHandPlacedTags(AppSettings.Defaults.HandPlacedBusTags);
            Assert.Empty(_store.Saved);
            presenter.RememberHandPlacedTags("sp, jnc, BFSEC:jn");
            Assert.Equal("sp, jnc, BFSEC:jn", _store.Saved.Single().HandPlacedBusTags);
            Assert.Equal("sp, jnc, BFSEC:jn", presenter.Current.HandPlacedBusTags);
        }

        [Fact]
        public void Save_KeepsHandPlacedTags()
        {
            _store.LoadResult = new SettingsLoadResult(AppSettings.Defaults with { HandPlacedBusTags = "sp, jnc, -AFSEC:jnc" }, null);
            var presenter = new SettingsPresenter(_view, _store);
            presenter.Load();
            presenter.OnSaveClicked();
            Assert.Equal("sp, jnc, -AFSEC:jnc", _store.Saved.Single().HandPlacedBusTags);
        }

        [Fact]
        public void Revert_ShowsCurrentWithoutSaving()
        {
            _presenter.Load();
            _view.Entered = Custom;
            _presenter.OnRevertClicked();
            Assert.Equal(AppSettings.Defaults, _view.Shown);
            Assert.Empty(_store.Saved);
        }

        [Fact]
        public void Defaults_ShowsDefaultsWithoutSaving()
        {
            _store.LoadResult = new SettingsLoadResult(Custom, null);
            _presenter.Load();
            _presenter.OnDefaultsClicked();
            Assert.Equal(AppSettings.Defaults, _view.Shown);
            Assert.Empty(_store.Saved);
            Assert.Equal(Custom, _presenter.Current);
        }

        [Fact]
        public void RememberExportStyle_SavesOnlyWhenDifferent_UpdatesView()
        {
            _presenter.Load();
            _presenter.RememberExportStyle(ExportStyle.OneWorkbook);
            Assert.Empty(_store.Saved);
            _presenter.RememberExportStyle(ExportStyle.SeparateFiles);
            Assert.Equal(ExportStyle.SeparateFiles, _store.Saved.Single().Export);
            Assert.Equal(ExportStyle.SeparateFiles, _presenter.Current.Export);
            Assert.Equal(ExportStyle.SeparateFiles, _view.Shown!.Export);
        }
    }
}
