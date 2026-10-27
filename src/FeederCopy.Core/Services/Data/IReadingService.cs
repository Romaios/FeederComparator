using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Monitoring;

namespace FeederCopy.Core.Services.Data
{
    public interface IReadingService
    {
        void SaveMonth(BillMonth month, IReadOnlyList<ReadingRecord> readings);
        IReadOnlyList<int> GetYears();
        IReadOnlyList<ReadingRecord> GetYear(int year);
        IReadOnlyList<ReadingRecord> GetMonth(BillMonth month);
        IReadOnlyList<BillMonth> GetMonths();
        IReadOnlyList<FeederMonthTotals> GetFeederTotals(int year);
    }
}
