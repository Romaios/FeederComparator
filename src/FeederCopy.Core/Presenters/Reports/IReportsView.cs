using FeederCopy.Core.Models.Monitoring;

namespace FeederCopy.Core.Presenters.Reports
{
    public interface IReportsView
    {
        void ShowFilters(IReadOnlyList<int> years, int selectedYear, IReadOnlyList<string> measures, int selectedMeasure);
        void ShowRows(string title, string totalHeader, bool kwh, IReadOnlyList<FeederMonthsRow> rows, string summary);
        void ShowEmpty(string message);
        void ShowLoading(string message);
        void SetExportEnabled(bool enabled);
        string? PickSaveFile(string filter, string defaultName);
        void ShowError(string message);
        void ShowInfo(string message);
    }
}
