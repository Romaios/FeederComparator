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
    }
}
