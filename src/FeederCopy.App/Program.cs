using FeederCopy.App.Forms.Shell;
using FeederCopy.App.Services.Database;
using FeederCopy.App.Services.Updates;
using FeederCopy.App.Utils;
using Velopack;
using FeederCopy.Core.Services.Data;
using FeederCopy.Core.Services.Export;
using FeederCopy.Core.Services.Import;
using FeederCopy.Core.Services.Logging;
using FeederCopy.Core.Services.Settings;

namespace FeederCopy.App
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            VelopackApp.Build().Run();
            ApplicationConfiguration.Initialize();
            string folder = AppPaths.DataFolder;
            var log = new FileErrorLog(Path.Combine(folder, "logs"));

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
            {
                log.Error("Unhandled UI exception", e.Exception);
                MessageBox.Show($"Something went wrong:\n{e.Exception.Message}\n\nDetails were written to the logs folder.",
                    Dialogs.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    log.Error("Unhandled exception", ex);
            };

            AppServices services;
            try
            {
                var database = new Database(DatabaseConnection.Empty);
                services = new AppServices(
                    database,
                    new DatabaseSettingsStore(Path.Combine(folder, "database.json")),
                    new FeederService(database),
                    new ReadingService(database),
                    new AsBilledReader(),
                    new FeederExporter(),
                    new MonitoringExporter(),
                    new SettingsStore(Path.Combine(folder, "settings.json")),
                    new VelopackUpdateService(),
                    new UpdateSettingsStore(Path.Combine(folder, "updater-settings.json")),
                    log);
            }
            catch (Exception ex)
            {
                log.Error("Startup", ex);
                MessageBox.Show($"The program could not start:\n{ex.Message}\n\nInstall it in a folder you can write to, for example C:\\FeederCopy.",
                    Dialogs.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AppTheme.Use(services.Settings.Load().Settings.Theme);
            Application.Run(new MainForm(services));
        }
    }
}
