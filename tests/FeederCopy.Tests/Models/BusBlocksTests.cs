using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Tests.Models
{
    public sealed class BusBlocksTests
    {
        private static ConsumerEntry E(string? bus, string id) => new(bus, id);

        private static IReadOnlyList<BusBlockRow> Plan(params ConsumerEntry[] current)
        {
            IReadOnlyList<BusTagRule> rules = BusTags.ParseRules("sp, jnc");
            return BusBlocks.Plan(current, BusSorter.Sort(current, rules), rules);
        }

        [Fact]
        public void Plan_UnsortedBuses_RowsNowAndAfter()
        {
            ConsumerEntry[] current = [.. Enumerable.Range(1, 3).Select(i => E("ANC15", $"a{i}")), .. Enumerable.Range(1, 2).Select(i => E("ANC2", $"b{i}")), E("ANC1", "c1")];
            IReadOnlyList<BusBlockRow> rows = Plan(current);
            Assert.Equal(
                [("ANC15", 1, 3, 4, 6, 3, true), ("ANC2", 4, 5, 2, 3, 2, true), ("ANC1", 6, 6, 1, 1, 1, true)],
                rows.Select(r => (r.Bus, r.From, r.To, r.NewFrom, r.NewTo, r.Count, r.Moves)));
        }

        [Fact]
        public void Plan_InOrder_NothingMoves()
        {
            IReadOnlyList<BusBlockRow> rows = Plan(E("A1", "c1"), E("A1", "c2"), E("A2", "c3"));
            Assert.All(rows, r => Assert.False(r.Moves));
        }

        [Fact]
        public void Plan_HandPlacedTagShown_UnknownLabelled()
        {
            IReadOnlyList<BusBlockRow> rows = Plan(E("A1", "c1"), E("A1jnc2", "c2"), E(null, "c3"));
            Assert.Equal([(string?)null, "jnc", null], rows.Select(r => r.HandPlacedTag));
            Assert.Equal("Unknown", rows[2].Bus);
        }

        [Fact]
        public void Plan_SplitBusRuns_EachRunListed()
        {
            IReadOnlyList<BusBlockRow> rows = Plan(E("A1", "c1"), E("A2", "c2"), E("A1", "c3"));
            Assert.Equal([("A1", 1, 1), ("A2", 2, 2), ("A1", 3, 3)], rows.Select(r => (r.Bus, r.From, r.To)));
            Assert.Equal([(1, 1), (3, 3), (2, 2)], rows.Select(r => (r.NewFrom, r.NewTo)));
        }
    }
}
