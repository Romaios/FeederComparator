using System.Text.Json;
using System.Text.Json.Serialization;
using FeederCopy.Core.Models.Settings;

namespace FeederCopy.Core.Services.Settings
{
    public sealed class SettingsStore : ISettingsStore
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            RespectNullableAnnotations = true,
            Converters = { new JsonStringEnumConverter() },
        };

        private readonly string _path;

        public SettingsStore(string path) => _path = path;

        public SettingsLoadResult Load()
        {
            if (!File.Exists(_path))
                return new SettingsLoadResult(AppSettings.Defaults, null);

            try
            {
                AppSettings settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), Options)
                    ?? throw new JsonException("The file is empty.");
                IReadOnlyList<string> errors = SettingsValidator.Validate(settings);
                return errors.Count == 0
                    ? new SettingsLoadResult(settings, null)
                    : new SettingsLoadResult(AppSettings.Defaults, Problem(string.Join(" ", errors)));
            }
            catch (JsonException ex)
            {
                return new SettingsLoadResult(AppSettings.Defaults, Problem(ex.Message));
            }
        }

        public void Save(AppSettings settings) =>
            File.WriteAllText(_path, JsonSerializer.Serialize(settings, Options));

        private static string Problem(string detail) =>
            $"settings.json could not be used ({detail}). Default settings are in effect until you review them in the Settings tab and click Save.";
    }
}
