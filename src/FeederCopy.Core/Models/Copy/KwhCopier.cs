using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Core.Models.Copy
{
    public static class KwhCopier
    {
        public const string NotFound = "Not in As-Billed";

        public static CopyResult Fill(IReadOnlyList<FeederSource> feeders, KwhSnapshot snapshot, BillMonth month)
        {
            var filled = new List<FilledFeeder>();
            var totals = new List<FeederTotals>();
            var problems = new List<CopyProblem>();

            foreach (FeederSource feeder in feeders)
            {
                var rows = new List<FilledRow>();
                int copied = 0, notFound = 0, duplicates = 0, unreadable = 0;
                double total = 0;

                for (int i = 0; i < feeder.Consumers.Count; i++)
                {
                    ConsumerEntry entry = feeder.Consumers[i];
                    string id = entry.ConsumerId.Trim();
                    string bus = BusLabels.For(entry.BusId);
                    double? kwh = null;
                    string? problem;

                    if (!snapshot.ByAccount.TryGetValue(id, out IReadOnlyList<AsBilledEntry>? matches))
                    {
                        problem = NotFound;
                        notFound++;
                    }
                    else if (matches.Count > 1)
                    {
                        problem = $"Appears {matches.Count} times in As-Billed";
                        duplicates++;
                    }
                    else if (matches[0].Kwh is not double value)
                    {
                        problem = $"kWh could not be read in As-Billed (row {matches[0].RowNumber})";
                        unreadable++;
                    }
                    else
                    {
                        problem = null;
                        kwh = value;
                        total += value;
                        copied++;
                    }

                    rows.Add(new FilledRow(i + 1, bus, id, kwh, problem));
                    if (problem is not null)
                        problems.Add(new CopyProblem(feeder.Name, i + 1, bus, id, problem));
                }

                filled.Add(new FilledFeeder(feeder.Name, rows));
                totals.Add(new FeederTotals(feeder.Name, rows.Count, copied, notFound, duplicates, unreadable, total));
            }

            return new CopyResult(month, snapshot.FileName, filled, totals, problems);
        }
    }
}
