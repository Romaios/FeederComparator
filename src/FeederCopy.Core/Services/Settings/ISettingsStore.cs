using FeederCopy.Core.Models.Settings;

namespace FeederCopy.Core.Services.Settings
{
    public interface ISettingsStore
    {
        SettingsLoadResult Load();
        void Save(AppSettings settings);
    }
}
