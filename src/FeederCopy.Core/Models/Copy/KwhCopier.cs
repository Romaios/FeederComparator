using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Core.Models.Copy
{
    public static class KwhCopier
    {
        public static CopyResult Fill(IReadOnlyList<FeederSource> feeders, KwhSnapshot snapshot, BillMonth month)
        {
            var filled = new List<FilledFeeder>();
            var totals = new List<FeederTotals>();
            var problems = new List<CopyProblem>();

            foreach (FeederSource feeder in feeders)
            {
                var rows = new List<FilledRow>();
                int active = 0, copied = 0, missing = 0, inactiveBilled = 0, duplicates = 0, unreadable = 0;
                double total = 0;

                for (int i = 0; i < feeder.Consumers.Count; i++)
                {
                    ConsumerEntry entry = feeder.Consumers[i];
                    string id = entry.ConsumerId.Trim();
                    string bus = BusLabels.For(entry.BusId);
                    if (entry.Active)
                        active++;

                    (string status, double? kwh, string? problem) = Evaluate(entry.Active, snapshot.ByAccount.TryGetValue(id, out IReadOnlyList<AsBilledEntry>? matches) ? matches : []);
                    switch (status)
                    {
                        case StatusLabels.MissingReading: missing++; break;
                        case StatusLabels.InactiveButBilled: inactiveBilled++; break;
                        case StatusLabels.DuplicateInAsBilled: duplicates++; break;
                        case StatusLabels.Unreadable: unreadable++; break;
                    }
                    if (kwh is double value)
                    {
                        copied++;
                        total += value;
                    }

                    rows.Add(new FilledRow(i + 1, bus, id, status, kwh, problem));
                    if (problem is not null)
                        problems.Add(new CopyProblem(feeder.Name, i + 1, bus, id, problem));
                }

                filled.Add(new FilledFeeder(feeder.Name, rows));
                totals.Add(new FeederTotals(feeder.Name, rows.Count, active, rows.Count - active, copied, missing, inactiveBilled, duplicates, unreadable, total));
            }

            return new CopyResult(month, snapshot.FileName, filled, totals, problems);
        }

        public static IReadOnlyList<ReadingRecord> Readings(CopyResult result) =>
            result.Feeders
                .SelectMany(f => f.Rows)
                .Select(r => new ReadingRecord(r.ConsumerId, result.Month.Key, r.Kwh, r.Status))
                .ToList();

        private static (string Status, double? Kwh, string? Problem) Evaluate(bool active, IReadOnlyList<AsBilledEntry> matches)
        {
            if (matches.Count == 0)
                return active ? (StatusLabels.MissingReading, null, StatusLabels.MissingReading) : (StatusLabels.Inactive, null, null);
            if (matches.Count > 1)
                return (StatusLabels.DuplicateInAsBilled, null, $"Appears {matches.Count} times in As-Billed");
            if (matches[0].Kwh is not double value)
                return (StatusLabels.Unreadable, null, $"kWh could not be read in As-Billed (row {matches[0].RowNumber})");
            return active
                ? (StatusLabels.Active, value, null)
                : (StatusLabels.InactiveButBilled, value, StatusLabels.InactiveButBilled);
        }
    }
}
