using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Core.Presenters.Feeders
{
    public interface IFeedersView
    {
        void ShowFeeders(IReadOnlyList<FeederSummary> feeders, long? selectedId);
        void ShowNoFeeders();
        void ShowConsumers(FeederSummary feeder, IReadOnlyList<ConsumerRow> rows, IReadOnlyList<string> buses, IReadOnlyList<FeederSummary> otherFeeders);
        void ShowConsumerRows(IReadOnlyList<ConsumerRow> rows);
        void ShowPreview(ImportPreview preview);
        void HidePreview();
        void SetBusy(bool busy, string? message);
        void ClearAddForm();
        void ShowAddError(string? message);
        void SelectConsumer(string consumerId);
        void ShowSearchResult(string? message);
        void ShowCreateError(string? message);
        void ClearCreateForm();
        string? PickOpenFile(string title, string filter);
        bool Confirm(string title, string message);
        void ShowError(string message);
        void ShowInfo(string message);
    }
}
