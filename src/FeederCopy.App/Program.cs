using FeederCopy.App.Forms.Shell;
using FeederCopy.App.Utils;
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
            ApplicationConfiguration.Initialize();
            string folder = AppContext.BaseDirectory;
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
                var database = new Database(Path.Combine(folder, "feeders.db"));
                database.EnsureSchema();
                services = new AppServices(
                    new FeederService(database),
                    new AsBilledReader(),
                    new FeederExporter(),
                    new SettingsStore(Path.Combine(folder, "settings.json")),
                    log);
            }
            catch (Exception ex)
            {
                log.Error("Startup", ex);
                MessageBox.Show($"The program could not start:\n{ex.Message}\n\nInstall it in a folder you can write to, for example C:\\FeederCopy.",
                    Dialogs.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new MainForm(services));
        }
    }
}
