using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Presenters.Feeders;
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

    internal sealed class FakeFeedersView : IFeedersView
    {
        public IReadOnlyList<FeederSummary> Feeders { get; private set; } = [];
        public long? SelectedFeederId { get; private set; }
        public int NoFeedersShown { get; private set; }
        public FeederSummary? ConsumersFeeder { get; private set; }
        public IReadOnlyList<ConsumerRow> Rows { get; private set; } = [];
        public IReadOnlyList<string> Buses { get; private set; } = [];
        public IReadOnlyList<FeederSummary> OtherFeeders { get; private set; } = [];
        public FeederPreview? Preview { get; private set; }
        public List<(bool Busy, string? Message)> Busy { get; } = [];
        public int AddCleared { get; private set; }
        public string? AddError { get; private set; }
        public string? SelectedConsumer { get; private set; }
        public string? SearchResult { get; private set; }
        public string? NextOpenFile { get; set; }
        public bool ConfirmAnswer { get; set; } = true;
        public List<string> ConfirmMessages { get; } = [];
        public List<string> Errors { get; } = [];

        public void ShowFeeders(IReadOnlyList<FeederSummary> feeders, long? selectedId)
        {
            Feeders = feeders;
            SelectedFeederId = selectedId;
        }

        public void ShowNoFeeders()
        {
            NoFeedersShown++;
            Feeders = [];
            SelectedFeederId = null;
            ConsumersFeeder = null;
            Rows = [];
        }

        public void ShowConsumers(FeederSummary feeder, IReadOnlyList<ConsumerRow> rows, IReadOnlyList<string> buses, IReadOnlyList<FeederSummary> otherFeeders)
        {
            ConsumersFeeder = feeder;
            Rows = rows;
            Buses = buses;
            OtherFeeders = otherFeeders;
        }

        public void ShowPreview(FeederPreview preview) => Preview = preview;

        public void HidePreview() => Preview = null;

        public void SetBusy(bool busy, string? message) => Busy.Add((busy, message));

        public void ClearAddForm() => AddCleared++;

        public void ShowAddError(string? message) => AddError = message;

        public void SelectConsumer(string consumerId) => SelectedConsumer = consumerId;

        public void ShowSearchResult(string? message) => SearchResult = message;

        public string? PickOpenFile(string title, string filter) => NextOpenFile;

        public bool Confirm(string title, string message)
        {
            ConfirmMessages.Add(message);
            return ConfirmAnswer;
        }

        public void ShowError(string message) => Errors.Add(message);
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
