using FeederCopy.Core.Services.Data;
using FeederCopy.Core.Services.Export;
using FeederCopy.Core.Services.Import;
using FeederCopy.Core.Services.Logging;
using FeederCopy.Core.Services.Settings;

namespace FeederCopy.App
{
    public sealed record AppServices(
        IFeederService Feeders,
        IAsBilledReader AsBilled,
        IFeederExporter Exporter,
        ISettingsStore Settings,
        IErrorLog Log);
}
