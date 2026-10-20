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
                [new FeederTotals("New York Feeder", 120, 118, 2, 116, 1, 0, 1, 0, 15230.5)],
                [new CopyProblem("New York Feeder", 12, "A", "c1", "Missing reading")]);
            string[] lines = CopyReport.ToText(result).Split(Environment.NewLine);
            Assert.Equal("kWh copy for Jul 2026 from as billed 07-2026.xls", lines[0]);
            Assert.Contains("New York Feeder: 120 consumers (118 active, 2 inactive), 116 copied, 1 missing reading, 0 inactive but billed, 1 duplicate, 0 unreadable, 15,230.50 kWh", lines);
            Assert.Contains("New York Feeder · #12 · bus A · c1 · Missing reading", lines);
        }

        [Fact]
        public void ToText_NoProblems()
        {
            var result = new CopyResult(Jul, "x.xls", [], [new FeederTotals("F", 1, 1, 0, 1, 0, 0, 0, 0, 1)], []);
            Assert.EndsWith("No problems.", CopyReport.ToText(result).TrimEnd());
        }
    }
}
