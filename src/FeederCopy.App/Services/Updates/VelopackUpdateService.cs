using System.Reflection;
using FeederCopy.Core.Models.Updates;
using FeederCopy.Core.Services.Updates;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace FeederCopy.App.Services.Updates
{
    public sealed class VelopackUpdateService : IUpdateService
    {
        public bool IsInstalled => VelopackLocator.IsCurrentSet && VelopackLocator.Current.CurrentlyInstalledVersion is not null;

        public string CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? string.Empty;

        public async Task<UpdateOffer?> CheckAsync(UpdateSettings settings, CancellationToken cancellationToken)
        {
            string url = settings.RepositoryUrl ?? throw new InvalidOperationException("The update repository is not set.");
            var manager = new UpdateManager(new GithubSource(url, settings.Token, false));
            UpdateInfo? info = await manager.CheckForUpdatesAsync();
            if (info is null)
                return null;
            long size = info.DeltasToTarget.Any() ? info.DeltasToTarget.Sum(d => d.Size) : info.TargetFullRelease.Size;
            return new UpdateOffer(info.TargetFullRelease.Version.ToString(), size, new Package(manager, info));
        }

        public Task DownloadAsync(UpdateOffer offer, IProgress<int> progress, CancellationToken cancellationToken)
        {
            var package = (Package)offer.Package;
            return package.Manager.DownloadUpdatesAsync(package.Info, progress.Report, cancellationToken);
        }

        public void ApplyAndRestart(UpdateOffer offer)
        {
            var package = (Package)offer.Package;
            package.Manager.ApplyUpdatesAndRestart(package.Info.TargetFullRelease);
        }

        private sealed record Package(UpdateManager Manager, UpdateInfo Info);
    }
}
