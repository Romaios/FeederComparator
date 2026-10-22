using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Core.Models.Copy
{
    public static class KwhCopier
    {
        public const string SavedSource = "saved copy";

        private sealed record Evaluated(bool Active, string Status, double? Kwh, string? Problem);

        public static CopyResult Fill(IReadOnlyList<FeederSource> feeders, KwhSnapshot snapshot, BillMonth month, bool negativeAsZero = true) =>
            Build(feeders, month, snapshot.FileName, entry =>
                Evaluate(snapshot.ByAccount.TryGetValue(entry.ConsumerId.Trim(), out IReadOnlyList<AsBilledEntry>? matches) ? matches : [], negativeAsZero));

        public static CopyResult FromReadings(IReadOnlyList<FeederSource> feeders, IReadOnlyList<ReadingRecord> readings, BillMonth month)
        {
            Dictionary<string, ReadingRecord> byConsumer = readings
                .Where(r => r.BillMonth == month.Key)
                .GroupBy(r => r.ConsumerId, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            return Build(feeders, month, SavedSource, entry =>
                byConsumer.TryGetValue(entry.ConsumerId.Trim(), out ReadingRecord? reading) ? FromReading(reading) : NoBill());
        }

        public static IReadOnlyList<ReadingRecord> Readings(CopyResult result) =>
            result.Feeders
                .SelectMany(f => f.Rows)
                .Select(r => new ReadingRecord(r.ConsumerId, result.Month.Key, r.Kwh, r.Status))
                .ToList();

        public static IReadOnlyList<(string ConsumerId, bool Active)> Statuses(CopyResult result) =>
            result.Feeders.SelectMany(f => f.Rows).Select(r => (r.ConsumerId, r.Active)).ToList();

        public static bool IsActiveStatus(string status) =>
            status is not (StatusLabels.Inactive or StatusLabels.MissingReading);

        private static CopyResult Build(IReadOnlyList<FeederSource> feeders, BillMonth month, string source, Func<ConsumerEntry, Evaluated> evaluate)
        {
            var filled = new List<FilledFeeder>();
            var totals = new List<FeederTotals>();
            var problems = new List<CopyProblem>();

            foreach (FeederSource feeder in feeders)
            {
                List<Evaluated> evaluated = feeder.Consumers.Select(evaluate).ToList();
                int[] counts = RowCounts.For(evaluated.Select(e => e.Active).ToList());
                var rows = new List<FilledRow>(evaluated.Count);
                int copied = 0, duplicates = 0, unreadable = 0;
                double total = 0;

                for (int i = 0; i < evaluated.Count; i++)
                {
                    ConsumerEntry entry = feeder.Consumers[i];
                    Evaluated e = evaluated[i];
                    string id = entry.ConsumerId.Trim();
                    string bus = BusLabels.For(entry.BusId);
                    if (e.Status == StatusLabels.DuplicateInAsBilled)
                        duplicates++;
                    if (e.Status == StatusLabels.Unreadable)
                        unreadable++;
                    if (e.Kwh is double value)
                    {
                        copied++;
                        total += value;
                    }
                    rows.Add(new FilledRow(counts[i], bus, id, e.Active, e.Status, e.Kwh, e.Problem));
                    if (e.Problem is not null)
                        problems.Add(new CopyProblem(feeder.Name, counts[i], bus, id, e.Problem));
                }

                int active = rows.Count(r => r.Active);
                filled.Add(new FilledFeeder(feeder.Name, rows));
                totals.Add(new FeederTotals(feeder.Name, rows.Count, active, rows.Count - active, copied, duplicates, unreadable, total));
            }

            return new CopyResult(month, source, filled, totals, problems);
        }

        private static Evaluated NoBill() => new(false, StatusLabels.Inactive, null, null);

        private static Evaluated Evaluate(IReadOnlyList<AsBilledEntry> matches, bool negativeAsZero)
        {
            if (matches.Count == 0)
                return NoBill();
            if (matches.Count > 1)
                return new(true, StatusLabels.DuplicateInAsBilled, null, $"Appears {matches.Count} times in As-Billed");
            AsBilledEntry match = matches[0];
            if (match.Blank)
                return NoBill();
            if (match.Kwh is not double value)
                return new(true, StatusLabels.Unreadable, null, $"kWh could not be read in As-Billed (row {match.RowNumber})");
            return new(true, StatusLabels.Active, negativeAsZero && value < 0 ? 0 : value, null);
        }

        private static Evaluated FromReading(ReadingRecord reading) => reading.Status switch
        {
            StatusLabels.DuplicateInAsBilled => new(true, reading.Status, null, "Appears more than once in As-Billed"),
            StatusLabels.Unreadable => new(true, reading.Status, null, "kWh could not be read in As-Billed"),
            _ when !IsActiveStatus(reading.Status) => new(false, StatusLabels.Inactive, null, null),
            _ => new(true, StatusLabels.Active, reading.Kwh, null),
        };
    }
}
