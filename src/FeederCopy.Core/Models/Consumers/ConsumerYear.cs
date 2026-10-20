using System.Globalization;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Core.Models.Consumers
{
    public sealed record ConsumerFilter(string? Feeder, bool? Active, string? Search)
    {
        public static ConsumerFilter All { get; } = new(null, null, null);
    }

    public sealed record ConsumerYearRow(string Feeder, string BusLabel, string ConsumerId, bool Active, IReadOnlyList<string> Months, double Total)
    {
        public string StatusLabel => StatusLabels.For(Active);
    }

    public static class ConsumerYearCalculator
    {
        public static IReadOnlyList<ConsumerYearRow> Build(
            IReadOnlyList<(string FeederName, ConsumerEntry Entry)> consumers,
            IReadOnlyList<ReadingRecord> readings,
            int year,
            ConsumerFilter filter)
        {
            string yearKey = year.ToString("D4", CultureInfo.InvariantCulture);
            Dictionary<string, ReadingRecord[]> byConsumer = readings
                .Where(r => r.BillMonth.StartsWith(yearKey, StringComparison.Ordinal))
                .GroupBy(r => r.ConsumerId, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);
            string? search = string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim();

            var rows = new List<ConsumerYearRow>();
            foreach ((string feeder, ConsumerEntry entry) in consumers)
            {
                if (filter.Feeder is not null && !string.Equals(feeder, filter.Feeder, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (filter.Active is bool active && entry.Active != active)
                    continue;
                if (search is not null && !entry.ConsumerId.Contains(search, StringComparison.OrdinalIgnoreCase))
                    continue;

                var months = new string[12];
                Array.Fill(months, string.Empty);
                double total = 0;
                foreach (ReadingRecord reading in byConsumer.GetValueOrDefault(entry.ConsumerId, []))
                {
                    int month = int.Parse(reading.BillMonth[4..], CultureInfo.InvariantCulture);
                    months[month - 1] = Cell(reading);
                    total += reading.Kwh ?? 0;
                }
                rows.Add(new ConsumerYearRow(feeder, BusLabels.For(entry.BusId), entry.ConsumerId, entry.Active, months, total));
            }
            return rows;
        }

        public static IReadOnlyList<int> Years(IReadOnlyList<int> savedYears, int currentYear) =>
            savedYears.Append(currentYear).Distinct().OrderDescending().ToList();

        public static int DefaultYear(IReadOnlyList<int> savedYears, int currentYear) =>
            savedYears.Count > 0 ? savedYears.Max() : currentYear;

        private static string Cell(ReadingRecord reading) => reading.Kwh is double kwh
            ? kwh.ToString("N2", CultureInfo.InvariantCulture)
            : reading.Status switch
            {
                StatusLabels.MissingReading => "Missing",
                StatusLabels.DuplicateInAsBilled => "Duplicate",
                StatusLabels.Unreadable => "Unreadable",
                _ => reading.Status,
            };
    }
}
