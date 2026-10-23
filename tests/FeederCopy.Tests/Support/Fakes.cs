using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Consumers;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Presenters.BusSort;
using FeederCopy.Core.Presenters.Consumers;
using FeederCopy.Core.Presenters.Database;
using FeederCopy.Core.Services.Data;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Presenters.Copy;
using FeederCopy.Core.Presenters.Feeders;
using FeederCopy.Core.Presenters.Settings;
using FeederCopy.Core.Services.Logging;
using FeederCopy.Core.Services.Settings;

namespace FeederCopy.Tests.Support
{
    internal sealed class FakeErrorLog : IErrorLog
    {
        public List<(string Context, Exception Exception)> Entries { get; } = [];

        public IEnumerable<string> Contexts => Entries.Select(e => e.Context);

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
        public ImportPreview? Preview { get; private set; }
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

        public int RowRefreshes { get; private set; }

        public void ShowConsumerRows(IReadOnlyList<ConsumerRow> rows)
        {
            Rows = rows;
            RowRefreshes++;
        }

        public void ShowPreview(ImportPreview preview) => Preview = preview;

        public void HidePreview() => Preview = null;

        public void SetBusy(bool busy, string? message) => Busy.Add((busy, message));

        public void ClearAddForm() => AddCleared++;

        public void ShowAddError(string? message) => AddError = message;

        public void SelectConsumer(string consumerId) => SelectedConsumer = consumerId;

        public void ShowSearchResult(string? message) => SearchResult = message;

        public string? CreateError { get; private set; }
        public int CreateCleared { get; private set; }

        public void ShowCreateError(string? message) => CreateError = message;

        public List<string> Infos { get; } = [];

        public void ShowInfo(string message) => Infos.Add(message);

        public void ClearCreateForm() => CreateCleared++;

        public string? PickOpenFile(string title, string filter) => NextOpenFile;

        public bool Confirm(string title, string message)
        {
            ConfirmMessages.Add(message);
            return ConfirmAnswer;
        }

        public void ShowError(string message) => Errors.Add(message);
    }

    internal sealed class FakeCopyView : ICopyView
    {
        private readonly object _gate = new();
        private readonly List<(bool Busy, string? Message)> _busy = [];

        public (int? Month, int? Year) Month { get; private set; }
        public IReadOnlyList<FeederTotals>? Totals { get; private set; }
        public IReadOnlyList<CopyProblem>? Problems { get; private set; }
        public string? Heading { get; private set; }
        public string? ClearedMessage { get; private set; }
        public ExportStyle? Style { get; private set; }
        public bool CopyEnabled { get; private set; }
        public bool ExportEnabled { get; private set; }
        public string? NextOpenFile { get; set; }
        public string? NextSaveFile { get; set; }
        public string? NextFolder { get; set; }
        public List<(string Filter, string DefaultName)> SavePrompts { get; } = [];
        public bool ConfirmAnswer { get; set; } = true;
        public List<(string Title, string Message)> Confirms { get; } = [];
        public List<string> Errors { get; } = [];
        public List<string> Infos { get; } = [];

        public IReadOnlyList<(bool Busy, string? Message)> Busy
        {
            get { lock (_gate) return _busy.ToList(); }
        }

        public string? FileText { get; private set; }
        public bool MonthEnabled { get; private set; } = true;
        public IReadOnlyList<string>? NextOpenFiles { get; set; }

        public void ShowFileText(string text) => FileText = text;

        public void SetMonthEnabled(bool enabled) => MonthEnabled = enabled;

        public void SetMonth(int? month, int? year) => Month = (month, year);

        public void ShowResult(IReadOnlyList<FeederTotals> totals, IReadOnlyList<CopyProblem> problems, string heading)
        {
            Totals = totals;
            Problems = problems;
            Heading = heading;
            ClearedMessage = null;
        }

        public void ClearResult(string message)
        {
            Totals = null;
            Problems = null;
            Heading = null;
            ClearedMessage = message;
        }

        public void SetExportStyle(ExportStyle style) => Style = style;

        public void SetBusy(bool busy, string? message)
        {
            lock (_gate)
                _busy.Add((busy, message));
        }

        public void SetCopyEnabled(bool enabled) => CopyEnabled = enabled;

        public void SetExportEnabled(bool enabled) => ExportEnabled = enabled;

        public IReadOnlyList<string> PickOpenFiles(string title, string filter) =>
            NextOpenFiles ?? (NextOpenFile is null ? [] : [NextOpenFile]);

        public string? PickSaveFile(string filter, string defaultName)
        {
            SavePrompts.Add((filter, defaultName));
            return NextSaveFile;
        }

        public string? PickFolder() => NextFolder;

        public bool Confirm(string title, string message)
        {
            Confirms.Add((title, message));
            return ConfirmAnswer;
        }

        public void ShowError(string message) => Errors.Add(message);

        public void ShowInfo(string message) => Infos.Add(message);
    }

    internal sealed class FakeConsumersView : IConsumersView
    {
        public IReadOnlyList<string> FeederNames { get; private set; } = [];
        public IReadOnlyList<int> Years { get; private set; } = [];
        public int SelectedYear { get; private set; }
        public IReadOnlyList<ConsumerYearRow> Rows { get; private set; } = [];
        public string? Summary { get; private set; }
        public string? EmptyMessage { get; private set; }
        public string? SelectedConsumer { get; private set; }
        public List<string> Errors { get; } = [];

        public BillMonth SuggestedInactiveMonth { get; private set; }
        public (int Active, int Inactive) StatusCounts { get; private set; }

        public void ShowFilters(IReadOnlyList<string> feederNames, IReadOnlyList<int> years, int selectedYear, BillMonth suggestedInactiveMonth)
        {
            FeederNames = feederNames;
            Years = years;
            SelectedYear = selectedYear;
            SuggestedInactiveMonth = suggestedInactiveMonth;
        }

        public void ShowRows(IReadOnlyList<ConsumerYearRow> rows, string summary, int activeCount, int inactiveCount)
        {
            Rows = rows;
            Summary = summary;
            StatusCounts = (activeCount, inactiveCount);
            EmptyMessage = null;
        }

        public void ShowEmpty(string message)
        {
            Rows = [];
            EmptyMessage = message;
        }

        public void SelectConsumer(string consumerId) => SelectedConsumer = consumerId;

        public IReadOnlyList<string> SelectedConsumers { get; private set; } = [];

        public void SelectConsumers(IReadOnlyList<string> consumerIds) => SelectedConsumers = consumerIds;

        public bool ConfirmAnswer { get; set; } = true;
        public List<string> Confirms { get; } = [];

        public bool Confirm(string title, string message)
        {
            Confirms.Add(message);
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

        public bool ConfirmAnswer { get; set; } = true;
        public List<string> Confirms { get; } = [];
        public bool Restarted { get; private set; }

        public bool Confirm(string title, string message)
        {
            Confirms.Add(message);
            return ConfirmAnswer;
        }

        public void RestartApplication() => Restarted = true;
    }

    internal sealed class FakeBusSortView : IBusSortView
    {
        public IReadOnlyList<FeederSummary> Feeders { get; private set; } = [];
        public long? SelectedFeederId { get; private set; }
        public string? Summary { get; private set; }
        public IReadOnlyList<BusBlockRow> Blocks { get; private set; } = [];
        public IReadOnlyList<BusTagRow> Tags { get; private set; } = [];
        public bool HasChanges { get; private set; }
        public IReadOnlyList<int> SelectedBlocks { get; private set; } = [];
        public string? EmptyMessage { get; private set; }
        public int PlansShown { get; private set; }
        public bool ConfirmAnswer { get; set; } = true;
        public List<string> ConfirmMessages { get; } = [];
        public List<string> Infos { get; } = [];
        public List<string> Errors { get; } = [];

        public void ShowFeeders(IReadOnlyList<FeederSummary> feeders, long selectedId)
        {
            Feeders = feeders;
            SelectedFeederId = selectedId;
        }

        public void ShowPlan(string summary, IReadOnlyList<BusBlockRow> blocks, IReadOnlyList<BusTagRow> tags, bool hasChanges)
        {
            Summary = summary;
            Blocks = blocks;
            Tags = tags;
            HasChanges = hasChanges;
            EmptyMessage = null;
            PlansShown++;
        }

        public void SelectBlocks(IReadOnlyList<int> indexes) => SelectedBlocks = indexes;

        public void ShowEmpty(string message) => EmptyMessage = message;

        public bool Confirm(string title, string message)
        {
            ConfirmMessages.Add(message);
            return ConfirmAnswer;
        }

        public void ShowInfo(string message) => Infos.Add(message);

        public void ShowError(string message) => Errors.Add(message);
    }

    internal sealed class FakeDatabaseView : IDatabaseView
    {
        public (string Host, int Port, string Database, string User, bool HasPassword)? Shown { get; private set; }
        public (string Message, DatabaseTone Tone)? Status { get; private set; }
        public int ScriptsOpened { get; private set; }

        public void ShowConnection(string host, int port, string database, string user, bool hasPassword) => Shown = (host, port, database, user, hasPassword);

        public void ShowStatus(string message, DatabaseTone tone) => Status = (message, tone);

        public void SetBusy(bool busy) { }

        public void OpenScriptsFolder() => ScriptsOpened++;
    }

    internal sealed class FakeDatabaseSettingsStore : IDatabaseSettingsStore
    {
        public DatabaseConnection Stored { get; set; } = DatabaseConnection.Empty;
        public int Saves { get; private set; }

        public DatabaseConnection Load() => Stored;

        public void Save(DatabaseConnection connection)
        {
            Stored = connection;
            Saves++;
        }
    }
}
