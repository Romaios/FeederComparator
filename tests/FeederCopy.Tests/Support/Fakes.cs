using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Presenters.Settings;
using FeederCopy.Core.Services.Logging;
using FeederCopy.Core.Services.Settings;

namespace FeederCopy.Tests.Support
{
    internal sealed class FakeErrorLog : IErrorLog
    {
        public List<(string Context, Exception Exception)> Entries { get; } = [];

        public void Error(string context, Exception exception) => Entries.Add((context, exception));
    }

    internal sealed class FakeSettingsStore : ISettingsStore
    {
        public SettingsLoadResult LoadResult { get; set; } = new(AppSettings.Defaults, null);
        public List<AppSettings> Saved { get; } = [];

        public SettingsLoadResult Load() => LoadResult;

        public void Save(AppSettings settings) => Saved.Add(settings);
    }

    internal sealed class FakeSettingsView : ISettingsView
    {
        public AppSettings? Shown { get; private set; }
        public AppSettings Entered { get; set; } = AppSettings.Defaults;
        public IReadOnlyList<string>? Validation { get; private set; }
        public int SavedCount { get; private set; }

        public void ShowSettings(AppSettings settings)
        {
            Shown = settings;
            Entered = settings;
        }

        public AppSettings ReadSettings() => Entered;

        public void ShowValidation(IReadOnlyList<string> errors) => Validation = errors;

        public void ShowSaved() => SavedCount++;
    }
}
