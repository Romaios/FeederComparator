using FeederCopy.Core.Models.Monitoring;

namespace FeederCopy.Core.Presenters.Monitoring
{
    public interface IMonitoringView
    {
        void ShowYears(IReadOnlyList<int> years, int selectedYear);
        void ShowSections(IReadOnlyList<MonitoringSection> sections, IReadOnlyList<int> months, string summary);
        void ShowEmpty(string message);
        void ShowLoading(string message);
        void SetExportEnabled(bool enabled);
        string? PickSaveFile(string filter, string defaultName);
        void ShowError(string message);
        void ShowInfo(string message);
    }
}
