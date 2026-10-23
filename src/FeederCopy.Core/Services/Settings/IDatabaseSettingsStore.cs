using FeederCopy.Core.Services.Data;

namespace FeederCopy.Core.Services.Settings
{
    public interface IDatabaseSettingsStore
    {
        DatabaseConnection Load();
        void Save(DatabaseConnection connection);
    }
}
