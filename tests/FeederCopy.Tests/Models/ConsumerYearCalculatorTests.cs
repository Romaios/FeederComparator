using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Consumers;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Tests.Models
{
    public sealed class ConsumerYearCalculatorTests
    {
        private static readonly (string, ConsumerEntry)[] Consumers =
        [
            ("Abuyog", new ConsumerEntry("A", "c1")),
            ("Abuyog", new ConsumerEntry(null, "c2", false)),
            ("Bacon", new ConsumerEntry("B", "0900001")),
        ];

        private static readonly ReadingRecord[] Readings =
        [
            new("c1", "202601", 100, "Active"),
            new("c1", "202602", null, "Missing reading"),
            new("c1", "202612", 50.5, "Active"),
            new("c2", "202601", null, "Inactive"),
            new("c2", "202602", 7, "Inactive but billed"),
            new("0900001", "202601", null, "Duplicate in As-Billed"),
            new("0900001", "202602", null, "kWh unreadable"),
        ];

        private static IReadOnlyList<ConsumerYearRow> Build(ConsumerFilter? filter = null) =>
            ConsumerYearCalculator.Build(Consumers, Readings, 2026, filter ?? ConsumerFilter.All);

        [Fact]
        public void Build_OneRowPerConsumer_InGivenOrder()
        {
            IReadOnlyList<ConsumerYearRow> rows = Build();
            Assert.Equal([("Abuyog", "A", "c1", "Active"), ("Abuyog", "Unknown", "c2", "Inactive"), ("Bacon", "B", "0900001", "Active")],
                rows.Select(r => (r.Feeder, r.BusLabel, r.ConsumerId, r.StatusLabel)));
        }

        [Fact]
        public void Build_MonthCells()
        {
            IReadOnlyList<ConsumerYearRow> rows = Build();
            Assert.Equal(12, rows[0].Months.Count);
            Assert.Equal("100.00", rows[0].Months[0]);
            Assert.Equal("Missing", rows[0].Months[1]);
            Assert.Equal("", rows[0].Months[2]);
            Assert.Equal("50.50", rows[0].Months[11]);
            Assert.Equal(["Inactive", "7.00"], rows[1].Months.Take(2));
            Assert.Equal(["Duplicate", "Unreadable"], rows[2].Months.Take(2));
        }

        [Fact]
        public void Build_TotalSumsKwh()
        {
            IReadOnlyList<ConsumerYearRow> rows = Build();
            Assert.Equal([150.5, 7, 0], rows.Select(r => r.Total));
        }

        [Fact]
        public void Build_FilterByFeederStatusSearch()
        {
            Assert.Equal(["c1", "c2"], Build(new ConsumerFilter("abuyog", null, null)).Select(r => r.ConsumerId));
            Assert.Equal(["c2"], Build(new ConsumerFilter(null, false, null)).Select(r => r.ConsumerId));
            Assert.Equal(["0900001"], Build(new ConsumerFilter(null, null, " 9000 ")).Select(r => r.ConsumerId));
        }

        [Fact]
        public void Build_CountIsRowInItsFeeder_EvenWhenFiltered()
        {
            Assert.Equal([1, 2, 1], Build().Select(r => r.Count));
            Assert.Equal([2], Build(new ConsumerFilter(null, false, null)).Select(r => r.Count));
            Assert.Equal([1], Build(new ConsumerFilter("Bacon", null, null)).Select(r => r.Count));
        }

        [Fact]
        public void Build_InactiveFrom_SetsLabelAndActiveMonths()
        {
            var inactiveFrom = new Dictionary<string, BillMonth> { ["c2"] = new(2026, 7), ["c1"] = new(2026, 3) };
            IReadOnlyList<ConsumerYearRow> rows = ConsumerYearCalculator.Build(Consumers, Readings, 2026, ConsumerFilter.All, inactiveFrom);
            Assert.Equal(["Active", "Inactive from Jul 2026", "Active"], rows.Select(r => r.StatusLabel));
            Assert.Equal([12, 6, 12], rows.Select(r => r.ActiveMonths));
            Assert.Null(rows[0].InactiveFrom);
            Assert.Equal(new BillMonth(2026, 7), rows[1].InactiveFrom);
        }

        [Fact]
        public void InactiveMonths_ByYear()
        {
            var july = new BillMonth(2026, 7);
            Assert.Equal(12, InactiveMonths.ActiveMonthsIn(true, null, 2026));
            Assert.Equal(0, InactiveMonths.ActiveMonthsIn(false, null, 2026));
            Assert.Equal(6, InactiveMonths.ActiveMonthsIn(false, july, 2026));
            Assert.Equal(12, InactiveMonths.ActiveMonthsIn(false, july, 2025));
            Assert.Equal(0, InactiveMonths.ActiveMonthsIn(false, july, 2027));
            Assert.Equal(0, InactiveMonths.ActiveMonthsIn(false, new BillMonth(2026, 1), 2026));
        }

        [Fact]
        public void InactiveMonths_IsActiveIn()
        {
            var july = new BillMonth(2026, 7);
            Assert.True(InactiveMonths.IsActiveIn(true, null, july));
            Assert.False(InactiveMonths.IsActiveIn(false, null, july));
            Assert.True(InactiveMonths.IsActiveIn(false, july, new BillMonth(2026, 6)));
            Assert.False(InactiveMonths.IsActiveIn(false, july, july));
            Assert.Equal("Inactive", InactiveMonths.Label(false, null));
        }

        [Fact]
        public void Years_SavedPlusCurrent_Descending()
        {
            Assert.Equal([2026, 2025], ConsumerYearCalculator.Years([2025], 2026));
            Assert.Equal([2026], ConsumerYearCalculator.Years([], 2026));
            Assert.Equal([2027, 2026], ConsumerYearCalculator.Years([2027, 2026], 2026));
        }

        [Fact]
        public void DefaultYear_LatestSavedOtherwiseCurrent()
        {
            Assert.Equal(2025, ConsumerYearCalculator.DefaultYear([2025], 2026));
            Assert.Equal(2026, ConsumerYearCalculator.DefaultYear([], 2026));
        }

        [Fact]
        public void Build_ProblemFilter()
        {
            Assert.Equal(["0900001"], Build(new ConsumerFilter(null, null, null, ConsumerProblem.Duplicate)).Select(r => r.ConsumerId));
            Assert.Equal(["0900001"], Build(new ConsumerFilter(null, null, null, ConsumerProblem.Unreadable)).Select(r => r.ConsumerId));
            Assert.Equal(["c1", "c2"], Build(new ConsumerFilter(null, null, null, ConsumerProblem.NoBill)).Select(r => r.ConsumerId));
            Assert.Equal(["c1", "c2", "0900001"], Build(new ConsumerFilter(null, null, null, ConsumerProblem.Any)).Select(r => r.ConsumerId));
            Assert.Equal(3, Build(new ConsumerFilter(null, null, null, ConsumerProblem.All)).Count);
        }
    }
}
