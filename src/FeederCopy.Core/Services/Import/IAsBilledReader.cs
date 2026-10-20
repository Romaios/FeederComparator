using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Settings;

namespace FeederCopy.Core.Services.Import
{
    public interface IAsBilledReader
    {
        KwhSnapshot Read(string path, AsBilledColumns columns, IProgress<int>? progress);
    }
}
