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


    public sealed record FeederMonthsRow(string Feeder, IReadOnlyList<double?> Months, double? Total, bool IsTotal);

    public sealed record MonitoringSection(MonitoringMeasure Measure, string Title, bool Kwh, IReadOnlyList<FeederMonthsRow> Rows);

    public static class MonitoringTable
    {
        public const string AllFeeders = "All feeders";

        public static IReadOnlyList<string> MonthHeaders { get; } = ["JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"];

        public static IReadOnlyList<MonitoringMeasure> Measures { get; } = Enum.GetValues<MonitoringMeasure>();

        public static IReadOnlyList<MonitoringMeasure> MonitoringMeasures { get; } = [MonitoringMeasure.TotalKwh, MonitoringMeasure.Active, MonitoringMeasure.Inactive];

        public static string Label(MonitoringMeasure measure) => measure switch
        {
            MonitoringMeasure.TotalKwh => "Total kWh",
            MonitoringMeasure.Active => "Active consumers",
            MonitoringMeasure.Inactive => "Inactive consumers",
            MonitoringMeasure.Consumers => "All consumers",
            MonitoringMeasure.Duplicates => "Duplicates in As-Billed",
            _ => "kWh unreadable",
        };

        public static string SectionTitle(MonitoringMeasure measure) => measure switch
        {
            MonitoringMeasure.TotalKwh => "TOTAL KWH",
            MonitoringMeasure.Active => "CONSUMERS ACTIVE",
            MonitoringMeasure.Inactive => "CONSUMERS INACTIVE",
            _ => Label(measure).ToUpperInvariant(),
        };

        public static bool IsKwh(MonitoringMeasure measure) => measure == MonitoringMeasure.TotalKwh;

        public const string TotalHeader = "Total";

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

        public static IReadOnlyList<int> MonthsWithData(IReadOnlyList<FeederMonthTotals> totals) =>
            totals.Select(t => t.Month.Month).Distinct().Order().ToList();

        public static IReadOnlyList<MonitoringSection> Sections(IReadOnlyList<string> feeders, IReadOnlyList<FeederMonthTotals> totals, IReadOnlyList<MonitoringMeasure> measures) =>
            measures.Select(m => new MonitoringSection(m, SectionTitle(m), IsKwh(m), ByMonth(feeders, totals, m))).ToList();

        public static IReadOnlyList<FeederMonthsRow> ByMonth(IReadOnlyList<string> feeders, IReadOnlyList<FeederMonthTotals> totals, MonitoringMeasure measure)
        {
            var rows = new List<FeederMonthsRow>();
            foreach (string feeder in FeederOrder(feeders, totals))
            {
                var months = new double?[12];
                foreach (FeederMonthTotals t in totals.Where(t => string.Equals(t.Feeder, feeder, StringComparison.OrdinalIgnoreCase)))
                    months[t.Month.Month - 1] = Value(t, measure);
                rows.Add(new FeederMonthsRow(feeder, months, Total(months), false));
            }

            var all = new double?[12];
            for (int m = 0; m < 12; m++)
            {
                List<double> values = rows.Where(r => r.Months[m].HasValue).Select(r => r.Months[m]!.Value).ToList();
                all[m] = values.Count > 0 ? values.Sum() : null;
            }
            rows.Add(new FeederMonthsRow(AllFeeders, all, Total(all), true));
            return rows;
        }

        private static double? Total(IReadOnlyList<double?> months) =>
            months.Any(v => v.HasValue) ? months.Sum(v => v ?? 0) : null;

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
