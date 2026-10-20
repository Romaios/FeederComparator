using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;

namespace FeederCopy.Tests.Models
{
    public sealed class ExportNamesTests
    {
        [Fact]
        public void SheetNames_ReplacesInvalidChars() =>
            Assert.Equal(["A-B-C-"], ExportNames.SheetNames(["A/B:C?"]));

        [Fact]
        public void SheetNames_CutsTo31()
        {
            string name = ExportNames.SheetNames([new string('x', 40)])[0];
            Assert.Equal(31, name.Length);
        }

        [Fact]
        public void SheetNames_MakesUnique() =>
            Assert.Equal(["X-Y", "X-Y (2)"], ExportNames.SheetNames(["X/Y", "X:Y"]));

        [Fact]
        public void SheetNames_UniqueStaysWithin31()
        {
            IReadOnlyList<string> names = ExportNames.SheetNames([new string('x', 40), new string('x', 35)]);
            Assert.Equal(2, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.All(names, n => Assert.True(n.Length <= 31));
        }

        [Fact]
        public void SheetNames_UniqueIgnoresCase() =>
            Assert.Equal(["Feeder A", "feeder a (2)"], ExportNames.SheetNames(["Feeder A", "feeder a"]));

        [Fact]
        public void SheetNames_BlankBecomesFeeder() =>
            Assert.Equal(["Feeder"], ExportNames.SheetNames(["  "]));

        [Fact]
        public void SheetNames_StripsLeadingTrailingApostrophe() =>
            Assert.Equal(["North"], ExportNames.SheetNames(["'North'"]));

        [Fact]
        public void FeederFileName_RemovesInvalidFileChars() =>
            Assert.Equal("North-South - Jul 2026.xlsx", ExportNames.FeederFileName("North/South", new BillMonth(2026, 7)));

        [Fact]
        public void FeederFileNames_UniqueWhenCleaningCollides() =>
            Assert.Equal(["A-B - Jul 2026.xlsx", "A-B (2) - Jul 2026.xlsx"], ExportNames.FeederFileNames(["A/B", "A:B"], new BillMonth(2026, 7)));

        [Fact]
        public void WorkbookFileName_HasMonth() =>
            Assert.Equal("Feeders - Jul 2026.xlsx", ExportNames.WorkbookFileName(new BillMonth(2026, 7)));
    }
}
