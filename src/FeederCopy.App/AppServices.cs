using FeederCopy.Core.Services.Data;
using FeederCopy.Core.Services.Export;
using FeederCopy.Core.Services.Import;
using FeederCopy.Core.Services.Logging;
using FeederCopy.Core.Services.Settings;
using FeederCopy.Core.Services.Updates;

namespace FeederCopy.App
{
    public sealed record AppServices(
        IFeederService Feeders,
        IReadingService Readings,
        IAsBilledReader AsBilled,
        IFeederExporter Exporter,
        ISettingsStore Settings,
        IUpdateService Updater,
        IUpdateSettingsStore UpdateSettings,
        IErrorLog Log);
}
