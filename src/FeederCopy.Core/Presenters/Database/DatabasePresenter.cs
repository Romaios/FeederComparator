using FeederCopy.Core.Models;
using FeederCopy.Core.Services.Data;
using FeederCopy.Core.Services.Logging;
using FeederCopy.Core.Services.Settings;
using DataStore = FeederCopy.Core.Services.Data.Database;

namespace FeederCopy.Core.Presenters.Database
{
    public sealed class DatabasePresenter
    {
        private const string NotSet = "Enter the MySQL host, port, database name, user and password, then click Save.";

        private readonly IDatabaseView _view;
        private readonly IDatabaseSettingsStore _store;
        private readonly DataStore _database;
        private readonly ILegacyImporter _importer;
        private readonly IErrorLog _log;

        public DatabasePresenter(IDatabaseView view, IDatabaseSettingsStore store, DataStore database, ILegacyImporter importer, IErrorLog log)
        {
            _view = view;
            _store = store;
            _database = database;
            _importer = importer;
            _log = log;
        }

        public event Action? Connected;

        public event Action? DataImported;

        public bool IsReady { get; private set; }

        public string? Load()
        {
            DatabaseConnection connection;
            try
            {
                connection = _store.Load();
            }
            catch (Exception ex)
            {
                _log.Error("Loading the database settings", ex);
                connection = DatabaseConnection.Empty;
                _view.ShowConnection(connection.Host, connection.Port, connection.Database, connection.User, false);
                return Report($"The saved database settings could not be read ({ex.Message}). Enter them again in Settings → Database.", DatabaseTone.Error);
            }
            _database.Use(connection);
            _view.ShowConnection(connection.Host, connection.Port, connection.Database, connection.User, connection.Password.Length > 0);
            return CheckCurrent();
        }

        public void OnTestClicked(DatabaseForm form)
        {
            (DatabaseConnection? connection, string? error) = Build(form);
            if (connection is null)
            {
                _view.ShowStatus(error!, DatabaseTone.Error);
                return;
            }
            _view.SetBusy(true);
            try
            {
                (string message, DatabaseTone tone, _) = Describe(new DataStore(connection));
                _view.ShowStatus(message, tone);
            }
            finally
            {
                _view.SetBusy(false);
            }
        }

        public void OnSaveClicked(DatabaseForm form)
        {
            (DatabaseConnection? connection, string? error) = Build(form);
            if (connection is null)
            {
                _view.ShowStatus(error!, DatabaseTone.Error);
                return;
            }
            try
            {
                _store.Save(connection);
            }
            catch (Exception ex)
            {
                _log.Error("Saving the database settings", ex);
                _view.ShowStatus($"Could not save the database settings. {ex.Message}", DatabaseTone.Error);
                return;
            }
            _database.Use(connection);
            _view.ShowConnection(connection.Host, connection.Port, connection.Database, connection.User, connection.Password.Length > 0);
            CheckCurrent();
            if (IsReady)
                Connected?.Invoke();
        }

        public void OnOpenScriptsClicked() => _view.OpenScriptsFolder();

        public async Task OnImportOldDataClickedAsync()
        {
            if (!IsReady)
            {
                _view.ShowImportResult("Connect to a database with its tables ready first (Save, then Test connection), then copy the old data.", DatabaseTone.Warning);
                return;
            }
            string? path = _view.PickOldDatabase();
            if (path is null)
                return;

            _view.SetBusy(true);
            _view.ShowImportResult($"Copying the old data from {Path.GetFileName(path)}… A big file can take about a minute.", DatabaseTone.Info);
            LegacyImportResult result;
            try
            {
                result = await Task.Run(() => _importer.Import(path));
            }
            catch (LegacyImportException ex)
            {
                _log.Error($"Copying old data from {path}", ex);
                _view.ShowImportResult(ex.Message, DatabaseTone.Error);
                return;
            }
            catch (Exception ex)
            {
                _log.Error($"Copying old data from {path}", ex);
                _view.ShowImportResult($"Could not copy the old data. Nothing was saved. {ex.Message}", DatabaseTone.Error);
                return;
            }
            finally
            {
                _view.SetBusy(false);
            }

            _view.ShowImportResult(
                $"Copied {Text.Count(result.Feeders, "feeder", "feeders")} and {Text.Count(result.Consumers, "consumer", "consumers")} ({result.Inactive:N0} inactive), "
                + $"with {Text.Count(result.Months, "month", "months")} of saved kWh.",
                DatabaseTone.Ok);
            DataImported?.Invoke();
        }

        private string? CheckCurrent()
        {
            _view.SetBusy(true);
            try
            {
                (string message, DatabaseTone tone, bool ready) = Describe(_database);
                IsReady = ready;
                _view.ShowStatus(message, tone);
                return ready ? null : message;
            }
            finally
            {
                _view.SetBusy(false);
            }
        }

        private string Report(string message, DatabaseTone tone)
        {
            IsReady = false;
            _view.ShowStatus(message, tone);
            return message;
        }

        private (string Message, DatabaseTone Tone, bool Ready) Describe(DataStore database)
        {
            if (!database.Connection.IsComplete)
                return (NotSet, DatabaseTone.Warning, false);
            try
            {
                DatabaseCheck check = database.Check();
                if (check.Ready)
                    return ($"Connected to MySQL {check.Version} · database {database.Connection.Database} · tables ready.", DatabaseTone.Ok, true);
                return ($"Connected to MySQL {check.Version}, but these scripts have not been run on database {database.Connection.Database} yet: {string.Join(", ", check.MissingScripts)}. Run them in MySQL Workbench (click Open scripts folder), then click Test.",
                    DatabaseTone.Warning, false);
            }
            catch (Exception ex)
            {
                _log.Error($"Connecting to MySQL {database.Connection.Host}:{database.Connection.Port}/{database.Connection.Database}", ex);
                return ($"Could not connect to MySQL at {database.Connection.Host}:{database.Connection.Port}. {ex.Message}", DatabaseTone.Error, false);
            }
        }

        private (DatabaseConnection? Connection, string? Error) Build(DatabaseForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Host))
                return (null, "Enter the MySQL host, for example localhost.");
            if (!int.TryParse(form.Port.Trim(), out int port) || port is < 1 or > 65535)
                return (null, "Enter a port number between 1 and 65535 (MySQL normally uses 3306).");
            if (string.IsNullOrWhiteSpace(form.Database))
                return (null, "Enter the database name.");
            if (form.Database.Trim().Any(ch => !(char.IsLetterOrDigit(ch) || ch is '_' or '$')))
                return (null, "The database name can only contain letters, numbers, _ and $.");
            if (string.IsNullOrWhiteSpace(form.User))
                return (null, "Enter the MySQL user name.");
            string password = form.Password.Length > 0 ? form.Password : _database.Connection.Password;
            return (new DatabaseConnection(form.Host.Trim(), port, form.Database.Trim(), form.User.Trim(), password), null);
        }
    }
}
