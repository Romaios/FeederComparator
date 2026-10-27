using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Monitoring;
using FeederCopy.Core.Services.Data;

namespace FeederCopy.Tests.Services
{
    public sealed class FeederTotalsCacheTests
    {
        [Fact]
        public async Task Get_SameYearAtOnce_ReadsOnce()
        {
            var readings = new TotalsReadings();
            var cache = new FeederTotalsCache(readings);
            IReadOnlyList<FeederMonthTotals>[] results = await Task.WhenAll(Task.Run(() => cache.Get(2026)), Task.Run(() => cache.Get(2026)));
            Assert.Equal(1, readings.Reads);
            Assert.Same(results[0], results[1]);
            cache.Get(2026);
            Assert.Equal(1, readings.Reads);
        }

        [Fact]
        public void Get_OtherYear_ReadsThatYear()
        {
            var readings = new TotalsReadings();
            var cache = new FeederTotalsCache(readings);
            Assert.Equal(2026, cache.Get(2026).Single().Month.Year);
            Assert.Equal(2025, cache.Get(2025).Single().Month.Year);
            Assert.Equal(2, readings.Reads);
        }

        [Fact]
        public void Clear_ReadsAgain()
        {
            var readings = new TotalsReadings();
            var cache = new FeederTotalsCache(readings);
            cache.Get(2026);
            cache.Clear();
            cache.Get(2026);
            Assert.Equal(2, readings.Reads);
        }

        [Fact]
        public void Get_Fails_DoesNotKeepTheFailure()
        {
            var readings = new TotalsReadings { FailNext = true };
            var cache = new FeederTotalsCache(readings);
            Assert.Throws<InvalidOperationException>(() => cache.Get(2026));
            Assert.Single(cache.Get(2026));
            Assert.Equal(2, readings.Reads);
        }

        private sealed class TotalsReadings : IReadingService
        {
            private int _reads;

            public int Reads => _reads;

            public bool FailNext { get; set; }

            public IReadOnlyList<FeederMonthTotals> GetFeederTotals(int year)
            {
                Interlocked.Increment(ref _reads);
                Thread.Sleep(50);
                if (FailNext)
                {
                    FailNext = false;
                    throw new InvalidOperationException("Server went away.");
                }
                return [new FeederMonthTotals("Abuyog", new BillMonth(year, 1), 1, 1, 0, 0, 0, 5)];
            }

            public void SaveMonth(BillMonth month, IReadOnlyList<ReadingRecord> readings) => throw new NotSupportedException();

            public IReadOnlyList<int> GetYears() => throw new NotSupportedException();

            public IReadOnlyList<ReadingRecord> GetYear(int year) => throw new NotSupportedException();

            public IReadOnlyList<ReadingRecord> GetMonth(BillMonth month) => throw new NotSupportedException();

            public IReadOnlyList<BillMonth> GetMonths() => throw new NotSupportedException();
        }
    }
}
