using System.Text.Json;
using FeederCopy.Core.Models.Updates;
using FeederCopy.Core.Services.Updates;

namespace FeederCopy.App.Services.Updates
{
    public sealed class UpdateSettingsStore : IUpdateSettingsStore
    {
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
        private readonly string _path;

        public UpdateSettingsStore(string path) => _path = path;

        public UpdateSettings Load()
        {
            if (!File.Exists(_path))
                return UpdateSettings.Empty;
            StoredSettings stored = JsonSerializer.Deserialize<StoredSettings>(File.ReadAllText(_path), Options)
                ?? throw new InvalidDataException("updater-settings.json is empty.");
            return new UpdateSettings(
                string.IsNullOrWhiteSpace(stored.Repository) ? UpdateSettings.DefaultRepository : stored.Repository,
                WindowsDataProtection.Unprotect(stored.ProtectedToken ?? string.Empty));
        }

        public void Save(UpdateSettings settings)
        {
            var stored = new StoredSettings
            {
                Repository = settings.Repository.Trim(),
                ProtectedToken = WindowsDataProtection.Protect(settings.Token),
            };
            string temp = _path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(stored, Options));
            File.Move(temp, _path, overwrite: true);
        }

        private sealed class StoredSettings
        {
            public string? Repository { get; set; }
            public string? ProtectedToken { get; set; }
        }
    }
}
