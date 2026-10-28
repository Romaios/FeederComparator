using FeederCopy.Core.Models.Updates;
using FeederCopy.Core.Presenters.Updates;
using FeederCopy.Core.Services.Updates;

namespace FeederCopy.Tests.Support
{
    internal sealed class FakeUpdatesView : IUpdatesView
    {
        public (string Repository, bool HasToken, string Version)? Source { get; private set; }
        public (string Message, bool IsError)? SourceMessage { get; private set; }
        public List<UpdateScreen> Screens { get; } = [];
        public UpdateScreen Screen => Screens[^1];
        public List<(int Percent, string Detail)> Progress { get; } = [];
        public void ShowSource(string repository, bool hasToken, string currentVersion) => Source = (repository, hasToken, currentVersion);
        public void ShowSourceMessage(string message, bool isError) => SourceMessage = (message, isError);
        public void ShowScreen(UpdateScreen screen) => Screens.Add(screen);
        public void ShowProgress(int percent, string detail) => Progress.Add((percent, detail));
        public string? Message { get; private set; }
        public void ShowMessage(string message) => Message = message;
        public string? Detail { get; private set; }
        public void ShowDetail(string detail) => Detail = detail;
    }

    internal sealed class FakeUpdateService : IUpdateService
    {
        public bool IsInstalled { get; set; } = true;
        public string CurrentVersion { get; set; } = "2.3.0";
        public UpdateOffer? Offer { get; set; }
        public Exception? CheckError { get; set; }
        public Exception? DownloadError { get; set; }
        public Action? DuringDownload { get; set; }
        public Action? DuringCheck { get; set; }
        public int Checks { get; private set; }
        public UpdateSettings? CheckedWith { get; private set; }
        public UpdateOffer? Applied { get; private set; }
        public Action? DuringApply { get; set; }

        public Task<UpdateOffer?> CheckAsync(UpdateSettings settings, CancellationToken cancellationToken)
        {
            Checks++;
            CheckedWith = settings;
            DuringCheck?.Invoke();
            return CheckError is null ? Task.FromResult(Offer) : Task.FromException<UpdateOffer?>(CheckError);
        }

        public Task DownloadAsync(UpdateOffer offer, IProgress<int> progress, CancellationToken cancellationToken)
        {
            DuringDownload?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            if (DownloadError is not null)
                throw DownloadError;
            return Task.CompletedTask;
        }

        public void ApplyAndRestart(UpdateOffer offer)
        {
            DuringApply?.Invoke();
            Applied = offer;
        }
    }

    internal sealed class FakeUpdateSettingsStore : IUpdateSettingsStore
    {
        public UpdateSettings Stored { get; set; } = UpdateSettings.Empty;
        public Exception? LoadError { get; set; }
        public UpdateSettings? Saved { get; private set; }

        public UpdateSettings Load() => LoadError is null ? Stored : throw LoadError;

        public void Save(UpdateSettings settings)
        {
            Saved = settings;
            Stored = settings;
        }
    }
}
