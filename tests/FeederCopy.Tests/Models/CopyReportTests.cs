using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;

namespace FeederCopy.Tests.Models
{
    public sealed class CopyReportTests
    {
        private static readonly BillMonth Jul = new(2026, 7);

        [Fact]
        public void ToText_HeaderTotalsAndProblems()
        {
            var result = new CopyResult(Jul, "as billed 07-2026.xls", [],
                [new FeederTotals("New York Feeder", 120, 118, 2, 116, 1, 0, 15230.5)],
                [new CopyProblem("New York Feeder", 12, "A", "c1", "Appears 2 times in As-Billed")]);
            string[] lines = CopyReport.ToText(result).Split(Environment.NewLine);
            Assert.Equal("kWh copy for Jul 2026 from as billed 07-2026.xls", lines[0]);
            Assert.Contains("New York Feeder: 120 consumers (118 active, 2 inactive with no bill), 116 copied, 1 duplicate, 0 unreadable, 15,230.50 kWh", lines);
            Assert.Contains("New York Feeder · #12 · bus A · c1 · Appears 2 times in As-Billed", lines);
        }

        [Fact]
        public void ToText_NoProblems()
        {
            var result = new CopyResult(Jul, "x.xls", [], [new FeederTotals("F", 1, 1, 0, 1, 0, 0, 1)], []);
            Assert.EndsWith("No problems.", CopyReport.ToText(result).TrimEnd());
            Assert.StartsWith("F: 1 consumer (1 active, 0 inactive with no bill)", CopyReport.TotalsLine(result.Totals[0]));
        }
    }
}
