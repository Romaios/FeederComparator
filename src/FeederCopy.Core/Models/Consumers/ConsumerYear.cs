using System.Globalization;
using System.Runtime.InteropServices;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Core.Models.Consumers
{
    public enum ConsumerProblem
    {
        All,
        Duplicate,
        Unreadable,
        NoBill,
        Any,
    }

    public sealed record ConsumerFilter(string? Feeder, bool? Active, string? Search, ConsumerProblem Problem = ConsumerProblem.All)
    {
        public static ConsumerFilter All { get; } = new(null, null, null);
    }

    public sealed record ConsumerYearRow(
        string Feeder,
        int Count,
        string BusLabel,
        string ConsumerId,
        bool Active,
        BillMonth? InactiveFrom,
        int ActiveMonths,
        IReadOnlyList<string> Months,
        double Total)
    {
        public string StatusLabel => InactiveMonths.Label(Active, InactiveFrom);
    }

    public static class InactiveMonths
    {
        public static bool IsActiveIn(bool active, BillMonth? inactiveFrom, BillMonth month) =>
            active || (inactiveFrom is { } from && month < from);

        public static int ActiveMonthsIn(bool active, BillMonth? inactiveFrom, int year)
        {
            if (active)
                return 12;
            if (inactiveFrom is not { } from)
                return 0;
            return Math.Clamp(from.Index - new BillMonth(year, 1).Index, 0, 12);
        }

        public static string Label(bool active, BillMonth? inactiveFrom) =>
            active ? StatusLabels.Active
            : inactiveFrom is { } from ? $"{StatusLabels.Inactive} from {from}"
            : StatusLabels.Inactive;
    }

    public sealed record ConsumerYearReadings(int Year, IReadOnlyDictionary<string, (IReadOnlyList<string> Months, double Total)> ByConsumer);

    public static class ConsumerYearCalculator
    {
        private static readonly IReadOnlyList<string> NoMonths = Enumerable.Repeat(string.Empty, 12).ToArray();

        public static ConsumerYearReadings Index(IReadOnlyList<ReadingRecord> readings, int year)
        {
            string yearKey = year.ToString("D4", CultureInfo.InvariantCulture);
            var byConsumer = new Dictionary<string, (string[] Months, double Total)>(StringComparer.Ordinal);
            foreach (ReadingRecord reading in readings)
            {
                if (!reading.BillMonth.StartsWith(yearKey, StringComparison.Ordinal))
                    continue;
                ref (string[] Months, double Total) entry = ref CollectionsMarshal.GetValueRefOrAddDefault(byConsumer, reading.ConsumerId, out bool exists);
                if (!exists)
                {
                    entry.Months = new string[12];
                    Array.Fill(entry.Months, string.Empty);
                }
                int month = int.Parse(reading.BillMonth.AsSpan(4), CultureInfo.InvariantCulture);
                entry.Months[month - 1] = Cell(reading);
                entry.Total += reading.Kwh ?? 0;
            }
            return new ConsumerYearReadings(year, byConsumer.ToDictionary(
                pair => pair.Key, pair => ((IReadOnlyList<string>)pair.Value.Months, pair.Value.Total), StringComparer.Ordinal));
        }

        public static IReadOnlyList<ConsumerYearRow> Build(
            IReadOnlyList<(string FeederName, ConsumerEntry Entry)> consumers,
            IReadOnlyList<ReadingRecord> readings,
            int year,
            ConsumerFilter filter,
            IReadOnlyDictionary<string, BillMonth>? inactiveFrom = null) =>
            Build(consumers, Index(readings, year), filter, inactiveFrom);

        public static IReadOnlyList<ConsumerYearRow> Build(
            IReadOnlyList<(string FeederName, ConsumerEntry Entry)> consumers,
            ConsumerYearReadings readings,
            ConsumerFilter filter,
            IReadOnlyDictionary<string, BillMonth>? inactiveFrom = null)
        {
            string? search = string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim();
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var rows = new List<ConsumerYearRow>();
            foreach ((string feeder, ConsumerEntry entry) in consumers)
            {
                int seen = counts.GetValueOrDefault(feeder);
                int count = seen + 1;
                if (entry.Active)
                    counts[feeder] = count;
                if (filter.Feeder is not null && !string.Equals(feeder, filter.Feeder, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (filter.Active is bool active && entry.Active != active)
                    continue;
                if (search is not null && !entry.ConsumerId.Contains(search, StringComparison.OrdinalIgnoreCase))
                    continue;

                (IReadOnlyList<string> months, double total) = readings.ByConsumer.TryGetValue(entry.ConsumerId, out var found) ? found : (NoMonths, 0);
                if (entry.Active && months.Contains(StatusLabels.Inactive))
                    months = months.Select(cell => cell == StatusLabels.Inactive ? MissingCell : cell).ToArray();
                if (!HasProblem(months, filter.Problem))
                    continue;
                BillMonth? from = !entry.Active && inactiveFrom is not null && inactiveFrom.TryGetValue(entry.ConsumerId, out BillMonth m) ? m : null;
                rows.Add(new ConsumerYearRow(feeder, count, BusLabels.For(entry.BusId), entry.ConsumerId, entry.Active, from,
                    InactiveMonths.ActiveMonthsIn(entry.Active, from, readings.Year), months, total));
            }
            return rows;
        }

        public static bool HasProblem(IReadOnlyList<string> months, ConsumerProblem problem) => problem switch
        {
            ConsumerProblem.All => true,
            ConsumerProblem.Duplicate => months.Contains(DuplicateCell),
            ConsumerProblem.Unreadable => months.Contains(UnreadableCell),
            ConsumerProblem.NoBill => months.Any(IsNoBill),
            _ => months.Any(m => m == DuplicateCell || m == UnreadableCell || IsNoBill(m)),
        };

        public const string MissingCell = "Missing";

        private const string DuplicateCell = "Duplicate";
        private const string UnreadableCell = "Unreadable";

        private static bool IsNoBill(string cell) => cell is StatusLabels.Inactive or MissingCell;

        public static IReadOnlyList<int> Years(IReadOnlyList<int> savedYears, int currentYear) =>
            savedYears.Append(currentYear).Distinct().OrderDescending().ToList();

        public static int DefaultYear(IReadOnlyList<int> savedYears, int currentYear) =>
            savedYears.Count > 0 ? savedYears.Max() : currentYear;

        private static string Cell(ReadingRecord reading) => reading.Kwh is double kwh
            ? kwh.ToString("N2", CultureInfo.InvariantCulture)
            : reading.Status switch
            {
                StatusLabels.MissingReading => StatusLabels.Inactive,
                StatusLabels.DuplicateInAsBilled => DuplicateCell,
                StatusLabels.Unreadable => UnreadableCell,
                _ => reading.Status,
            };
    }
}
