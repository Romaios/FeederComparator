using FeederCopy.Core.Models.Updates;

namespace FeederCopy.Core.Services.Updates
{
    public interface IUpdateService
    {
        bool IsInstalled { get; }
        string CurrentVersion { get; }
        Task<UpdateOffer?> CheckAsync(UpdateSettings settings, CancellationToken cancellationToken);
        Task DownloadAsync(UpdateOffer offer, IProgress<int> progress, CancellationToken cancellationToken);
        void ApplyAndRestart(UpdateOffer offer);
    }
}
