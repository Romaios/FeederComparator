namespace FeederCopy.Core.Models.Updates
{
    public sealed record UpdateSettings(string Repository, string Token)
    {
        public const string DefaultRepository = "Romaios/FeederCopy";

        public static UpdateSettings Empty { get; } = new(DefaultRepository, string.Empty);

        public string? RepositoryUrl
        {
            get
            {
                string[] parts = Repository.Trim().Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return parts.Length == 2 ? $"https://github.com/{parts[0]}/{parts[1]}" : null;
            }
        }

        public bool IsConfigured => RepositoryUrl is not null && Token.Length > 0;
    }

    public sealed record UpdateOffer(string Version, long SizeBytes, object Package);

    public enum UpdateStage
    {
        Idle,
        NotInstalled,
        NeedsSettings,
        Checking,
        UpToDate,
        Available,
        Downloading,
        Cancelling,
        Applying,
        Failed,
    }

    public sealed record UpdateScreen(
        UpdateStage Stage, string Title, string Message, bool CanCheck, bool CanUpdate, bool CanCancel, bool ShowProgress);
}
