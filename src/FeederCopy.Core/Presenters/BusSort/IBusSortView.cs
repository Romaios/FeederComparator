using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Core.Presenters.BusSort
{
    public interface IBusSortView
    {
        void ShowFeeders(IReadOnlyList<FeederSummary> feeders, long selectedId);
        void ShowPlan(string summary, IReadOnlyList<BusBlockRow> blocks, IReadOnlyList<BusTagRow> tags, bool hasChanges);
        void SelectBlocks(IReadOnlyList<int> indexes);
        void ShowEmpty(string message);
        bool Confirm(string title, string message);
        void ShowInfo(string message);
        void ShowError(string message);
    }
}
