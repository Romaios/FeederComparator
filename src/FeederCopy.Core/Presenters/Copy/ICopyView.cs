using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Settings;

namespace FeederCopy.Core.Presenters.Copy
{
    public interface ICopyView
    {
        void ShowFileText(string text);
        void SetMonth(int? month, int? year);
        void SetMonthEnabled(bool enabled);
        void ShowResult(IReadOnlyList<FeederTotals> totals, IReadOnlyList<CopyProblem> problems, string heading);
        void ClearResult(string message);
        void SetExportStyle(ExportStyle style);
        void SetBusy(bool busy, string? message);
        void SetCopyEnabled(bool enabled);
        void SetExportEnabled(bool enabled);
        IReadOnlyList<string> PickOpenFiles(string title, string filter);
        string? PickSaveFile(string filter, string defaultName);
        string? PickFolder();
        bool Confirm(string title, string message);
        void ShowError(string message);
        void ShowInfo(string message);
    }
}
