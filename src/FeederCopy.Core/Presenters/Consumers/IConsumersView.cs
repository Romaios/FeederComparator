using FeederCopy.Core.Models.Consumers;

namespace FeederCopy.Core.Presenters.Consumers
{
    public interface IConsumersView
    {
        void ShowFilters(IReadOnlyList<string> feederNames, IReadOnlyList<int> years, int selectedYear);
        void ShowRows(IReadOnlyList<ConsumerYearRow> rows, string summary);
        void ShowEmpty(string message);
        void SelectConsumer(string consumerId);
        void ShowError(string message);
    }
}
