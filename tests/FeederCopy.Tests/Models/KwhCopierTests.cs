using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Tests.Models
{
    public sealed class KwhCopierTests
    {
        private static readonly BillMonth Jul = new(2026, 7);

        private static KwhSnapshot Snapshot(params (string Account, int Row, double? Kwh)[] rows) =>
            new("as billed.xls", Jul,
                rows.GroupBy(r => r.Account).ToDictionary(g => g.Key, g => (IReadOnlyList<AsBilledEntry>)g.Select(r => new AsBilledEntry(r.Row, r.Kwh)).ToList()),
                rows.Length, [], null);

        private static ConsumerEntry E(string? bus, string id) => new(bus, id);

        private static FeederSource Feeder(string name, params ConsumerEntry[] consumers) => new(name, consumers);

        private static FilledRow OnlyRow(CopyResult r) => r.Feeders.Single().Rows.Single();

        [Fact]
        public void Fill_FoundOnce_CopiesValue()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", "c1"))], Snapshot(("c1", 2, 150.5)), Jul);
            Assert.Equal(new FilledRow(1, "A", "c1", 150.5, null), OnlyRow(r));
        }

        [Fact]
        public void Fill_ZeroAndNegative_CopiedAsIs()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", "c1"), E("A", "c2"))], Snapshot(("c1", 2, 0), ("c2", 3, -5)), Jul);
            Assert.Equal([0d, -5d], r.Feeders[0].Rows.Select(x => x.Kwh!.Value));
            Assert.All(r.Feeders[0].Rows, x => Assert.Null(x.Problem));
            Assert.Empty(r.Problems);
        }

        [Fact]
        public void Fill_TrimmedMatch()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", " c1 "))], Snapshot(("c1", 2, 7)), Jul);
            Assert.Equal(7, OnlyRow(r).Kwh);
        }

        [Fact]
        public void Fill_NotFound()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", "c1"))], Snapshot(("c9", 2, 7)), Jul);
            Assert.Null(OnlyRow(r).Kwh);
            Assert.Equal("Not in As-Billed", OnlyRow(r).Problem);
        }

        [Fact]
        public void Fill_Duplicate()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", "c1"))], Snapshot(("c1", 2, 7), ("c1", 9, 8)), Jul);
            Assert.Null(OnlyRow(r).Kwh);
            Assert.Equal("Appears 2 times in As-Billed", OnlyRow(r).Problem);
        }

        [Fact]
        public void Fill_Unreadable()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", "c1"))], Snapshot(("c1", 15, null)), Jul);
            Assert.Null(OnlyRow(r).Kwh);
            Assert.Equal("kWh could not be read in As-Billed (row 15)", OnlyRow(r).Problem);
        }

        [Fact]
        public void Fill_KeepsOrderCountAndUnknownLabel()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("B", "c2"), E("A", "c1"), E(null, "c3"))], Snapshot(("c1", 2, 1), ("c2", 3, 2), ("c3", 4, 3)), Jul);
            Assert.Equal([(1, "B", "c2"), (2, "A", "c1"), (3, "Unknown", "c3")], r.Feeders[0].Rows.Select(x => (x.Count, x.BusLabel, x.ConsumerId)));
        }

        [Fact]
        public void Fill_Totals()
        {
            CopyResult r = KwhCopier.Fill(
                [Feeder("F", E("A", "c1"), E("A", "c2"), E("A", "c3"), E("A", "c4"), E("A", "c5"))],
                Snapshot(("c1", 2, 10.25), ("c2", 3, 4.75), ("c3", 4, 1), ("c3", 5, 1), ("c4", 6, null)),
                Jul);
            Assert.Equal(new FeederTotals("F", 5, 2, 1, 1, 1, 15), r.Totals.Single());
        }

        [Fact]
        public void Fill_ProblemsListedInFeederThenRowOrder()
        {
            CopyResult r = KwhCopier.Fill(
                [Feeder("F1", E("A", "c1"), E("A", "c2")), Feeder("F2", E(null, "c3"))],
                Snapshot(("c9", 2, 1)),
                Jul);
            Assert.Equal(
                [new CopyProblem("F1", 1, "A", "c1", "Not in As-Billed"), new CopyProblem("F1", 2, "A", "c2", "Not in As-Billed"), new CopyProblem("F2", 1, "Unknown", "c3", "Not in As-Billed")],
                r.Problems);
        }

        [Fact]
        public void Fill_CarriesMonthAndSource()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", "c1"))], Snapshot(("c1", 2, 1)), new BillMonth(2026, 6));
            Assert.Equal(new BillMonth(2026, 6), r.Month);
            Assert.Equal("as billed.xls", r.SourceFile);
        }
    }
}


