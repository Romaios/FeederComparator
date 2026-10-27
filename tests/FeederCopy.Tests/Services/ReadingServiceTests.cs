using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Monitoring;
using FeederCopy.Core.Services.Data;

namespace FeederCopy.Tests.Services
{
    public sealed class ReadingServiceTests : IDisposable
    {
        private readonly TempDatabase _temp = new();
        private readonly ReadingService _service;

        public ReadingServiceTests() => _service = new ReadingService(_temp.Db);

        public void Dispose() => _temp.Dispose();

        [Fact]
        public void SaveMonth_ThenGetYear()
        {
            _service.SaveMonth(new BillMonth(2026, 7), [new ReadingRecord("c1", "202607", 5, "Active"), new ReadingRecord("c2", "202607", null, "Missing reading")]);
            Assert.Equal(
                [new ReadingRecord("c1", "202607", 5, "Active"), new ReadingRecord("c2", "202607", null, "Missing reading")],
                _service.GetYear(2026).OrderBy(r => r.ConsumerId));
        }

        [Fact]
        public void SaveMonth_Twice_ReplacesThatMonthOnly()
        {
            _service.SaveMonth(new BillMonth(2026, 6), [new ReadingRecord("c1", "202606", 1, "Active")]);
            _service.SaveMonth(new BillMonth(2026, 7), [new ReadingRecord("c1", "202607", 5, "Active"), new ReadingRecord("c2", "202607", 6, "Active")]);
            _service.SaveMonth(new BillMonth(2026, 7), [new ReadingRecord("c1", "202607", 9, "Active")]);
            IReadOnlyList<ReadingRecord> year = _service.GetYear(2026);
            Assert.Equal(2, year.Count);
            Assert.Contains(new ReadingRecord("c1", "202607", 9, "Active"), year);
            Assert.Contains(new ReadingRecord("c1", "202606", 1, "Active"), year);
        }

        [Fact]
        public void GetYears_DistinctDescending()
        {
            _service.SaveMonth(new BillMonth(2025, 12), [new ReadingRecord("c1", "202512", 1, "Active")]);
            _service.SaveMonth(new BillMonth(2026, 1), [new ReadingRecord("c1", "202601", 1, "Active")]);
            _service.SaveMonth(new BillMonth(2026, 2), [new ReadingRecord("c1", "202602", 1, "Active")]);
            Assert.Equal([2026, 2025], _service.GetYears());
            Assert.Single(_service.GetYear(2025));
        }

        [Fact]
        public void GetFeederTotals_GroupsByFeederAndMonth()
        {
            var feeders = new FeederService(_temp.Db);
            feeders.CreateFeeder("Bacon", [new ConsumerEntry("B", "c1"), new ConsumerEntry("B", "c2"), new ConsumerEntry("B", "c3"), new ConsumerEntry("B", "c4")]);
            feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c5")]);
            _service.SaveMonth(new BillMonth(2026, 7),
            [
                new ReadingRecord("c1", "202607", 10.5, "Active"),
                new ReadingRecord("c2", "202607", null, "Inactive"),
                new ReadingRecord("c3", "202607", null, "Duplicate in As-Billed"),
                new ReadingRecord("c4", "202607", null, "Missing reading"),
                new ReadingRecord("c5", "202607", 4, "Active"),
                new ReadingRecord("gone", "202607", 99, "Active"),
            ]);
            _service.SaveMonth(new BillMonth(2026, 8), [new ReadingRecord("c1", "202608", 2, "Active"), new ReadingRecord("c2", "202608", null, "kWh unreadable")]);
            _service.SaveMonth(new BillMonth(2025, 8), [new ReadingRecord("c1", "202508", 7, "Active")]);

            Assert.Equal(
            [
                new FeederMonthTotals("Abuyog", new BillMonth(2026, 7), 1, 1, 0, 0, 0, 4),
                new FeederMonthTotals("Bacon", new BillMonth(2026, 7), 4, 2, 2, 1, 0, 10.5),
                new FeederMonthTotals("Bacon", new BillMonth(2026, 8), 2, 2, 0, 0, 1, 2),
            ], _service.GetFeederTotals(2026));
            Assert.Empty(_service.GetFeederTotals(2024));
        }
    }
}
