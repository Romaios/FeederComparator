using System.Globalization;
using System.Text;

namespace FeederCopy.Core.Models.Copy
{
    public static class CopyReport
    {
        public static string ToText(CopyResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"kWh copy for {result.Month} from {result.SourceFile}");
            sb.AppendLine();
            foreach (FeederTotals t in result.Totals)
                sb.AppendLine(TotalsLine(t));
            sb.AppendLine();

            if (result.Problems.Count == 0)
            {
                sb.AppendLine("No problems.");
                return sb.ToString();
            }

            sb.AppendLine($"Problems ({result.Problems.Count}):");
            foreach (CopyProblem p in result.Problems)
                sb.AppendLine($"{p.Feeder} · #{p.Count} · bus {p.BusLabel} · {p.ConsumerId} · {p.Problem}");
            return sb.ToString();
        }

        public static string TotalsLine(FeederTotals t) =>
            $"{t.Name}: {(t.Consumers == 1 ? "1 consumer" : $"{t.Consumers} consumers")} ({t.Active} active, {t.Inactive} inactive with no bill), {t.Copied} copied, "
            + $"{t.Duplicates} {(t.Duplicates == 1 ? "duplicate" : "duplicates")}, {t.Unreadable} unreadable, "
            + $"{t.TotalKwh.ToString("N2", CultureInfo.InvariantCulture)} kWh";
    }
}
