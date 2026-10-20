using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
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
    }
}
