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
                [new FeederTotals("New York Feeder", 120, 117, 2, 1, 0, 15230.5)],
                [new CopyProblem("New York Feeder", 12, "A", "c1", "Not in As-Billed")]);
            string[] lines = CopyReport.ToText(result).Split(Environment.NewLine);
            Assert.Equal("kWh copy for Jul 2026 from as billed 07-2026.xls", lines[0]);
            Assert.Contains("New York Feeder: 120 consumers, 117 copied, 2 not found, 1 duplicate, 0 unreadable, 15,230.50 kWh", lines);
            Assert.Contains("New York Feeder · #12 · bus A · c1 · Not in As-Billed", lines);
        }

        [Fact]
        public void ToText_NoProblems()
        {
            var result = new CopyResult(Jul, "x.xls", [], [new FeederTotals("F", 1, 1, 0, 0, 0, 1)], []);
            Assert.EndsWith("No problems.", CopyReport.ToText(result).TrimEnd());
        }
    }
}
