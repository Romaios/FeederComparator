using Velopack.Locators;

namespace FeederCopy.App.Utils
{
    public static class AppPaths
    {
        private const string ContentFolderName = "current";
        private const string UpdaterExe = "Update.exe";

        public static string DataFolder { get; } = Resolve();

        private static string Resolve()
        {
            if (VelopackLocator.IsCurrentSet)
            {
                IVelopackLocator locator = VelopackLocator.Current;
                if (locator.CurrentlyInstalledVersion is not null && !string.IsNullOrWhiteSpace(locator.RootAppDir))
                    return locator.RootAppDir;
            }

            var appFolder = new DirectoryInfo(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory));
            if (string.Equals(appFolder.Name, ContentFolderName, StringComparison.OrdinalIgnoreCase)
                && appFolder.Parent is DirectoryInfo installFolder
                && File.Exists(Path.Combine(installFolder.FullName, UpdaterExe)))
                return installFolder.FullName;
            return appFolder.FullName;
        }
    }
}
