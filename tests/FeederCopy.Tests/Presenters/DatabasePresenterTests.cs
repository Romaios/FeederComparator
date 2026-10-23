using FeederCopy.Core.Presenters.Database;
using FeederCopy.Core.Services.Data;

namespace FeederCopy.Tests.Presenters
{
    public sealed class DatabasePresenterTests
    {
        private readonly FakeDatabaseView _view = new();
        private readonly FakeDatabaseSettingsStore _store = new();
        private readonly FakeErrorLog _log = new();
        private readonly Database _database = new(DatabaseConnection.Empty);
        private readonly DatabasePresenter _presenter;
        private int _connected;

        public DatabasePresenterTests()
        {
            _presenter = new DatabasePresenter(_view, _store, _database, _log);
            _presenter.Connected += () => _connected++;
        }

        private static DatabaseForm Form(DatabaseConnection c, string? password = null) =>
            new(c.Host, c.Port.ToString(), c.Database, c.User, password ?? c.Password);

        [Fact]
        public void Load_NothingSaved_AsksForTheConnection()
        {
            string? problem = _presenter.Load();
            Assert.Equal("Enter the MySQL host, port, database name, user and password, then click Save.", problem);
            Assert.Equal(("localhost", 3306, "", "", false), _view.Shown);
            Assert.Equal(DatabaseTone.Warning, _view.Status!.Value.Tone);
            Assert.False(_presenter.IsReady);
        }

        [Fact]
        public void Save_ReadyDatabase_SavesConnectsAndRaisesConnected()
        {
            using var temp = new TempDatabase();
            _presenter.Load();
            _presenter.OnSaveClicked(Form(temp.Db.Connection));
            Assert.Equal(temp.Db.Connection, _store.Stored);
            Assert.Equal(temp.Db.Connection, _database.Connection);
            Assert.True(_presenter.IsReady);
            Assert.Equal(1, _connected);
            Assert.Equal(DatabaseTone.Ok, _view.Status!.Value.Tone);
            Assert.EndsWith($"database {temp.Name} · tables ready.", _view.Status.Value.Message);
        }

        [Fact]
        public void Save_ScriptsNotRun_NamesTheScripts_NotConnected()
        {
            using var temp = new TempDatabase(runScripts: false);
            _presenter.OnSaveClicked(Form(temp.Db.Connection));
            Assert.Equal(1, _store.Saves);
            Assert.False(_presenter.IsReady);
            Assert.Equal(0, _connected);
            Assert.Equal(DatabaseTone.Warning, _view.Status!.Value.Tone);
            Assert.Contains("2026-10-23-01-create-tables", _view.Status.Value.Message);
        }

        [Fact]
        public void Save_BadPort_ShowsErrorSavesNothing()
        {
            _presenter.OnSaveClicked(new DatabaseForm("localhost", "abc", "feeders", "root", "x"));
            Assert.Equal(("Enter a port number between 1 and 65535 (MySQL normally uses 3306).", DatabaseTone.Error), _view.Status);
            Assert.Equal(0, _store.Saves);
        }

        [Fact]
        public void Save_BadDatabaseName_ShowsError()
        {
            _presenter.OnSaveClicked(new DatabaseForm("localhost", "3306", "feeders; drop", "root", "x"));
            Assert.Equal("The database name can only contain letters, numbers, _ and $.", _view.Status!.Value.Message);
            Assert.Equal(0, _store.Saves);
        }

        [Fact]
        public void Save_BlankPassword_KeepsSavedPassword()
        {
            _store.Stored = new DatabaseConnection("localhost", 3306, "feeders", "root", "secret");
            _presenter.Load();
            _presenter.OnSaveClicked(new DatabaseForm("localhost", "3306", "feeders2", "root", ""));
            Assert.Equal("secret", _store.Stored.Password);
            Assert.Equal("feeders2", _store.Stored.Database);
        }

        [Fact]
        public void Test_WrongPassword_ShowsErrorWithoutSaving()
        {
            using var temp = new TempDatabase();
            _presenter.OnTestClicked(Form(temp.Db.Connection, "wrong-" + Guid.NewGuid()));
            Assert.Equal(DatabaseTone.Error, _view.Status!.Value.Tone);
            Assert.StartsWith("Could not connect to MySQL at", _view.Status.Value.Message);
            Assert.Equal(0, _store.Saves);
            Assert.False(_presenter.IsReady);
        }

        [Fact]
        public void OpenScripts_AsksTheView()
        {
            _presenter.OnOpenScriptsClicked();
            Assert.Equal(1, _view.ScriptsOpened);
        }
    }
}
