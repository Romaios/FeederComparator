using FeederCopy.Core.Models.Updates;

namespace FeederCopy.Core.Services.Updates
{
    public interface IUpdateSettingsStore
    {
        UpdateSettings Load();
        void Save(UpdateSettings settings);
    }
}
