using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Monitoring;

namespace FeederCopy.Tests.Models
{
    public sealed class MonitoringTableTests
    {
        private static readonly IReadOnlyList<FeederMonthTotals> Totals =
        [
            new("Bacon", new BillMonth(2026, 1), 3, 2, 1, 0, 0, 100.5),
            new("Abuyog", new BillMonth(2026, 1), 2, 2, 0, 1, 0, 40),
            new("Bacon", new BillMonth(2026, 2), 3, 3, 0, 0, 1, 120),
        ];

        [Fact]
        public void Summary_EachMonthFeedersInOrderThenAllFeeders()
        {
            IReadOnlyList<MonthSummaryRow> rows = MonitoringTable.Summary(["Abuyog", "Bacon"], Totals);
            Assert.Equal(
            [
                new MonthSummaryRow(new BillMonth(2026, 1), "Abuyog", 40, 2, 0, false),
                new MonthSummaryRow(new BillMonth(2026, 1), "Bacon", 100.5, 2, 1, false),
                new MonthSummaryRow(new BillMonth(2026, 1), MonitoringTable.AllFeeders, 140.5, 4, 1, true),
                new MonthSummaryRow(new BillMonth(2026, 2), "Bacon", 120, 3, 0, false),
                new MonthSummaryRow(new BillMonth(2026, 2), MonitoringTable.AllFeeders, 120, 3, 0, true),
            ], rows);
        }

        [Fact]
        public void ByMonth_Kwh_TotalsAcrossMonthsAndFeeders()
        {
            IReadOnlyList<FeederMonthsRow> rows = MonitoringTable.ByMonth(["Abuyog", "Bacon", "Sorsogon"], Totals, MonitoringMeasure.TotalKwh);
            Assert.Equal(["Abuyog", "Bacon", "Sorsogon", MonitoringTable.AllFeeders], rows.Select(r => r.Feeder));
            Assert.Equal(100.5, rows[1].Months[0]);
            Assert.Equal(120, rows[1].Months[1]);
            Assert.Null(rows[1].Months[2]);
            Assert.Equal(220.5, rows[1].Total);
            Assert.All(rows[2].Months, Assert.Null);
            Assert.Null(rows[2].Total);
            Assert.Equal(140.5, rows[3].Months[0]);
            Assert.Equal(260.5, rows[3].Total);
            Assert.True(rows[3].IsTotal);
        }

        [Fact]
        public void ByMonth_Counts_AverageAcrossMonths()
        {
            IReadOnlyList<FeederMonthsRow> rows = MonitoringTable.ByMonth(["Bacon"], Totals, MonitoringMeasure.Active);
            Assert.Equal(["Bacon", "Abuyog", MonitoringTable.AllFeeders], rows.Select(r => r.Feeder));
            Assert.Equal(2.5, rows[0].Total);
            Assert.Equal(4, rows[2].Months[0]);
            Assert.Equal("Average", MonitoringTable.TotalHeader(MonitoringMeasure.Active));
            Assert.Equal("Total", MonitoringTable.TotalHeader(MonitoringMeasure.TotalKwh));
        }

        [Theory]
        [InlineData(MonitoringMeasure.Inactive, 1)]
        [InlineData(MonitoringMeasure.Consumers, 3)]
        [InlineData(MonitoringMeasure.Duplicates, 0)]
        [InlineData(MonitoringMeasure.Unreadable, 0)]
        public void Value_PicksTheMeasure(MonitoringMeasure measure, double expected) =>
            Assert.Equal(expected, MonitoringTable.Value(Totals[0], measure));
    }
}
