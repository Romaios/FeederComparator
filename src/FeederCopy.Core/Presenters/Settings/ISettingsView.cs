using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Settings;

namespace FeederCopy.Core.Presenters.Settings
{
    public interface ISettingsView
    {
        void ShowSettings(AppSettings settings);
        AppSettings ReadSettings();
        void ShowValidation(IReadOnlyList<string> errors);
        void ShowSaved();
        bool Confirm(string title, string message);
        void RestartApplication();
        void ShowBusTags(IReadOnlyList<string> feeders, string? selectedFeeder, IReadOnlyList<BusTagRow> rows);
    }
}
