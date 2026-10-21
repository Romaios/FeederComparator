using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Consumers;

namespace FeederCopy.Core.Presenters.Consumers
{
    public interface IConsumersView
    {
        void ShowFilters(IReadOnlyList<string> feederNames, IReadOnlyList<int> years, int selectedYear, BillMonth suggestedInactiveMonth);
        void ShowRows(IReadOnlyList<ConsumerYearRow> rows, string summary, int activeCount, int inactiveCount);
        void ShowEmpty(string message);
        void SelectConsumer(string consumerId);
        void ShowError(string message);
    }
}
