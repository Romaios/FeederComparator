using FeederCopy.Core.Models.Monitoring;

namespace FeederCopy.Core.Services.Data
{
    public sealed class FeederTotalsCache
    {
        private readonly IReadingService _readings;
        private readonly Lock _lock = new();
        private readonly Dictionary<int, Lazy<IReadOnlyList<FeederMonthTotals>>> _byYear = [];

        public FeederTotalsCache(IReadingService readings) => _readings = readings;

        public IReadOnlyList<FeederMonthTotals> Get(int year)
        {
            Lazy<IReadOnlyList<FeederMonthTotals>>? entry;
            lock (_lock)
            {
                if (!_byYear.TryGetValue(year, out entry))
                {
                    entry = new Lazy<IReadOnlyList<FeederMonthTotals>>(() => _readings.GetFeederTotals(year), LazyThreadSafetyMode.ExecutionAndPublication);
                    _byYear[year] = entry;
                }
            }
            try
            {
                return entry.Value;
            }
            catch
            {
                lock (_lock)
                {
                    if (_byYear.TryGetValue(year, out var current) && current == entry)
                        _byYear.Remove(year);
                }
                throw;
            }
        }

        public void Clear()
        {
            lock (_lock)
                _byYear.Clear();
        }
    }
}
