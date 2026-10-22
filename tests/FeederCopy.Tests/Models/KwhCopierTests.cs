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

        private static KwhSnapshot SnapshotOf(params (string Account, AsBilledEntry Entry)[] rows) =>
            new("as billed.xls", Jul,
                rows.GroupBy(r => r.Account).ToDictionary(g => g.Key, g => (IReadOnlyList<AsBilledEntry>)g.Select(r => r.Entry).ToList()),
                rows.Length, [], null);

        private static ConsumerEntry E(string? bus, string id, bool active = true) => new(bus, id, active);

        private static FeederSource Feeder(string name, params ConsumerEntry[] consumers) => new(name, consumers);

        private static FilledRow OnlyRow(CopyResult r) => r.Feeders.Single().Rows.Single();

        [Fact]
        public void Fill_Billed_ActiveAndCopied()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", "c1"))], Snapshot(("c1", 2, 150.5)), Jul);
            Assert.Equal(new FilledRow(1, "A", "c1", true, "Active", 150.5, null), OnlyRow(r));
            Assert.Empty(r.Problems);
        }

        [Fact]
        public void Fill_NotInAsBilled_InactiveForTheMonth_NoProblem()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", "c1"))], Snapshot(("c9", 2, 7)), Jul);
            Assert.Equal(new FilledRow(1, "A", "c1", false, "Inactive", null, null), OnlyRow(r));
            Assert.Empty(r.Problems);
        }

        [Fact]
        public void Fill_BlankKwh_InactiveForTheMonth()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", "c1"))], SnapshotOf(("c1", new AsBilledEntry(2, null, Blank: true))), Jul);
            Assert.Equal(new FilledRow(1, "A", "c1", false, "Inactive", null, null), OnlyRow(r));
        }

        [Fact]
        public void Fill_PreviouslyInactiveWithBill_ActiveAgain()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", "c1", false))], Snapshot(("c1", 2, 42)), Jul);
            Assert.Equal(new FilledRow(1, "A", "c1", true, "Active", 42, null), OnlyRow(r));
            Assert.Empty(r.Problems);
        }

        [Fact]
        public void Fill_ZeroReading_IsActive()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", "c1"))], Snapshot(("c1", 2, 0)), Jul);
            Assert.Equal(new FilledRow(1, "A", "c1", true, "Active", 0, null), OnlyRow(r));
        }

        [Fact]
        public void Fill_Negative_ZeroByDefault_KeptWhenSettingOff()
        {
            KwhSnapshot snapshot = Snapshot(("c1", 2, -5));
            Assert.Equal(0, OnlyRow(KwhCopier.Fill([Feeder("F", E("A", "c1"))], snapshot, Jul)).Kwh);
            FilledRow kept = OnlyRow(KwhCopier.Fill([Feeder("F", E("A", "c1"))], snapshot, Jul, negativeAsZero: false));
            Assert.Equal(-5, kept.Kwh);
            Assert.True(kept.Active);
        }

        [Fact]
        public void Fill_Duplicate_ActiveProblem()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", "c1"))], Snapshot(("c1", 2, 7), ("c1", 9, 8)), Jul);
            Assert.Equal(new FilledRow(1, "A", "c1", true, "Duplicate in As-Billed", null, "Appears 2 times in As-Billed"), OnlyRow(r));
        }

        [Fact]
        public void Fill_Unreadable_ActiveProblem()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", "c1", false))], Snapshot(("c1", 15, null)), Jul);
            Assert.Equal(new FilledRow(1, "A", "c1", true, "kWh unreadable", null, "kWh could not be read in As-Billed (row 15)"), OnlyRow(r));
        }

        [Fact]
        public void Fill_TrimmedMatch()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", " c1 "))], Snapshot(("c1", 2, 7)), Jul);
            Assert.Equal(7, OnlyRow(r).Kwh);
        }

        [Fact]
        public void Fill_CountSkipsInactive_InactiveShowsNextCount()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", "c1"), E("A", "c2"), E("A", "c3"), E(null, "c4"))], Snapshot(("c1", 2, 1), ("c3", 3, 2), ("c4", 4, 3)), Jul);
            Assert.Equal([(1, "A", "c1", true), (2, "A", "c2", false), (2, "A", "c3", true), (3, "Unknown", "c4", true)],
                r.Feeders[0].Rows.Select(x => (x.Count, x.BusLabel, x.ConsumerId, x.Active)));
        }

        [Fact]
        public void Fill_Totals()
        {
            CopyResult r = KwhCopier.Fill(
                [Feeder("F", E("A", "c1"), E("A", "c2"), E("A", "c3"), E("A", "c4"), E("A", "c5"), E("A", "c6", false), E("A", "c7", false))],
                Snapshot(("c1", 2, 10.25), ("c2", 3, 4.75), ("c3", 4, 1), ("c3", 5, 1), ("c4", 6, null), ("c7", 8, 2)),
                Jul);
            Assert.Equal(new FeederTotals("F", 7, 5, 2, 3, 1, 1, 17), r.Totals.Single());
        }

        [Fact]
        public void Fill_ProblemsListedInFeederThenRowOrder()
        {
            CopyResult r = KwhCopier.Fill(
                [Feeder("F1", E("A", "c1"), E("A", "c2")), Feeder("F2", E(null, "c3"))],
                Snapshot(("c1", 2, 1), ("c1", 3, 1), ("c2", 4, null), ("c3", 5, null)),
                Jul);
            Assert.Equal(
                [new CopyProblem("F1", 1, "A", "c1", "Appears 2 times in As-Billed"), new CopyProblem("F1", 2, "A", "c2", "kWh could not be read in As-Billed (row 4)"), new CopyProblem("F2", 1, "Unknown", "c3", "kWh could not be read in As-Billed (row 5)")],
                r.Problems);
        }

        [Fact]
        public void Fill_CarriesMonthAndSource()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", "c1"))], Snapshot(("c1", 2, 1)), new BillMonth(2026, 6));
            Assert.Equal(new BillMonth(2026, 6), r.Month);
            Assert.Equal("as billed.xls", r.SourceFile);
        }

        [Fact]
        public void Readings_AndStatuses_OnePerConsumer()
        {
            CopyResult r = KwhCopier.Fill([Feeder("F", E("A", "c1"), E("A", "c2"))], Snapshot(("c1", 2, 5)), Jul);
            Assert.Equal(
                [new ReadingRecord("c1", "202607", 5, "Active"), new ReadingRecord("c2", "202607", null, "Inactive")],
                KwhCopier.Readings(r));
            Assert.Equal([("c1", true), ("c2", false)], KwhCopier.Statuses(r));
        }

        [Fact]
        public void FromReadings_RebuildsSavedMonth()
        {
            ReadingRecord[] saved =
            [
                new("c1", "202607", 5, "Active"),
                new("c2", "202607", null, "Missing reading"),
                new("c3", "202607", null, "Duplicate in As-Billed"),
                new("c4", "202607", 9, "Inactive but billed"),
                new("c1", "202606", 99, "Active"),
            ];
            CopyResult r = KwhCopier.FromReadings([Feeder("F", E("A", "c1"), E("A", "c2"), E("A", "c3"), E("A", "c4"), E("A", "c5"))], saved, Jul);
            Assert.Equal(KwhCopier.SavedSource, r.SourceFile);
            Assert.Equal(
                [(1, true, (double?)5), (2, false, null), (2, true, null), (3, true, 9), (4, false, null)],
                r.Feeders[0].Rows.Select(x => (x.Count, x.Active, x.Kwh)));
            Assert.Equal(["c3"], r.Problems.Select(p => p.ConsumerId));
        }
    }
}
