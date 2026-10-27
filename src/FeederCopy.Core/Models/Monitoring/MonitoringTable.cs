using FeederCopy.Core.Models.Billing;

namespace FeederCopy.Core.Models.Monitoring
{
    public enum MonitoringMeasure
    {
        TotalKwh,
        Active,
        Inactive,
        Consumers,
        Duplicates,
        Unreadable,
    }

    public sealed record FeederMonthTotals(string Feeder, BillMonth Month, int Consumers, int Active, int Inactive, int Duplicates, int Unreadable, double TotalKwh);

    public sealed record MonthSummaryRow(BillMonth Month, string Feeder, double TotalKwh, int Active, int Inactive, bool IsTotal);

    public sealed record FeederMonthsRow(string Feeder, IReadOnlyList<double?> Months, double? Total, bool IsTotal);

    public static class MonitoringTable
    {
        public const string AllFeeders = "All feeders";

        public static IReadOnlyList<string> MonthHeaders { get; } = ["JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"];

        public static IReadOnlyList<MonitoringMeasure> Measures { get; } = Enum.GetValues<MonitoringMeasure>();

        public static string Label(MonitoringMeasure measure) => measure switch
        {
            MonitoringMeasure.TotalKwh => "Total kWh",
            MonitoringMeasure.Active => "Active consumers",
            MonitoringMeasure.Inactive => "Inactive consumers",
            MonitoringMeasure.Consumers => "All consumers",
            MonitoringMeasure.Duplicates => "Duplicates in As-Billed",
            _ => "kWh unreadable",
        };

        public static bool IsKwh(MonitoringMeasure measure) => measure == MonitoringMeasure.TotalKwh;

        public static string TotalHeader(MonitoringMeasure measure) => IsKwh(measure) ? "Total" : "Average";

        public static string SummaryFileName(int year) => $"Monitoring {year}.xlsx";

        public static string ReportFileName(int year) => $"Feeder report {year}.xlsx";

        public static double Value(FeederMonthTotals totals, MonitoringMeasure measure) => measure switch
        {
            MonitoringMeasure.TotalKwh => totals.TotalKwh,
            MonitoringMeasure.Active => totals.Active,
            MonitoringMeasure.Inactive => totals.Inactive,
            MonitoringMeasure.Consumers => totals.Consumers,
            MonitoringMeasure.Duplicates => totals.Duplicates,
            _ => totals.Unreadable,
        };

        public static IReadOnlyList<MonthSummaryRow> Summary(IReadOnlyList<string> feeders, IReadOnlyList<FeederMonthTotals> totals)
        {
            IReadOnlyList<string> order = FeederOrder(feeders, totals);
            var rows = new List<MonthSummaryRow>();
            foreach (IGrouping<BillMonth, FeederMonthTotals> month in totals.GroupBy(t => t.Month).OrderBy(g => g.Key))
            {
                var byFeeder = month.ToDictionary(t => t.Feeder, StringComparer.OrdinalIgnoreCase);
                foreach (string feeder in order)
                {
                    if (byFeeder.TryGetValue(feeder, out FeederMonthTotals? t))
                        rows.Add(new MonthSummaryRow(month.Key, t.Feeder, t.TotalKwh, t.Active, t.Inactive, false));
                }
                rows.Add(new MonthSummaryRow(month.Key, AllFeeders, month.Sum(t => t.TotalKwh), month.Sum(t => t.Active), month.Sum(t => t.Inactive), true));
            }
            return rows;
        }

        public static IReadOnlyList<FeederMonthsRow> ByMonth(IReadOnlyList<string> feeders, IReadOnlyList<FeederMonthTotals> totals, MonitoringMeasure measure)
        {
            var rows = new List<FeederMonthsRow>();
            foreach (string feeder in FeederOrder(feeders, totals))
            {
                var months = new double?[12];
                foreach (FeederMonthTotals t in totals.Where(t => string.Equals(t.Feeder, feeder, StringComparison.OrdinalIgnoreCase)))
                    months[t.Month.Month - 1] = Value(t, measure);
                rows.Add(new FeederMonthsRow(feeder, months, Total(months, measure), false));
            }

            var all = new double?[12];
            for (int m = 0; m < 12; m++)
            {
                List<double> values = rows.Where(r => r.Months[m].HasValue).Select(r => r.Months[m]!.Value).ToList();
                all[m] = values.Count > 0 ? values.Sum() : null;
            }
            rows.Add(new FeederMonthsRow(AllFeeders, all, Total(all, measure), true));
            return rows;
        }

        private static double? Total(IReadOnlyList<double?> months, MonitoringMeasure measure)
        {
            List<double> values = months.Where(v => v.HasValue).Select(v => v!.Value).ToList();
            if (values.Count == 0)
                return null;
            return IsKwh(measure) ? values.Sum() : values.Average();
        }

        private static IReadOnlyList<string> FeederOrder(IReadOnlyList<string> feeders, IReadOnlyList<FeederMonthTotals> totals)
        {
            var seen = new HashSet<string>(feeders, StringComparer.OrdinalIgnoreCase);
            var order = feeders.ToList();
            foreach (string feeder in totals.Select(t => t.Feeder))
            {
                if (seen.Add(feeder))
                    order.Add(feeder);
            }
            return order;
        }
    }
}
