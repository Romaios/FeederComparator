using System.Text.Json;
using FeederCopy.App.Services.Updates;
using FeederCopy.Core.Services.Data;
using FeederCopy.Core.Services.Settings;

namespace FeederCopy.App.Services.Database
{
    public sealed class DatabaseSettingsStore : IDatabaseSettingsStore
    {
        private const string PasswordPurpose = "FeederCopy.DatabasePassword.v1";
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
        private readonly string _path;

        public DatabaseSettingsStore(string path) => _path = path;

        public DatabaseConnection Load()
        {
            if (!File.Exists(_path))
                return DatabaseConnection.Empty;
            StoredConnection stored = JsonSerializer.Deserialize<StoredConnection>(File.ReadAllText(_path), Options)
                ?? throw new InvalidDataException("database.json is empty.");
            return new DatabaseConnection(
                string.IsNullOrWhiteSpace(stored.Host) ? DatabaseConnection.DefaultHost : stored.Host,
                stored.Port is > 0 and <= 65535 ? stored.Port : DatabaseConnection.DefaultPort,
                stored.Database ?? string.Empty,
                stored.User ?? string.Empty,
                WindowsDataProtection.Unprotect(stored.ProtectedPassword ?? string.Empty, PasswordPurpose));
        }

        public void Save(DatabaseConnection connection)
        {
            var stored = new StoredConnection
            {
                Host = connection.Host,
                Port = connection.Port,
                Database = connection.Database,
                User = connection.User,
                ProtectedPassword = WindowsDataProtection.Protect(connection.Password, PasswordPurpose),
            };
            string temp = _path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(stored, Options));
            File.Move(temp, _path, overwrite: true);
        }

        private sealed class StoredConnection
        {
            public string? Host { get; set; }
            public int Port { get; set; }
            public string? Database { get; set; }
            public string? User { get; set; }
            public string? ProtectedPassword { get; set; }
        }
    }
}
