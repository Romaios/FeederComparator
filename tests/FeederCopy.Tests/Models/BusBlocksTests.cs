using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Tests.Models
{
    public sealed class BusBlocksTests
    {
        private static readonly IReadOnlyList<BusTagRule> Rules = BusTags.ParseRules("sp, jnc");

        private static ConsumerEntry E(string? bus, string id) => new(bus, id);

        private static IReadOnlyList<BusBlockRow> Rows(IReadOnlyList<ConsumerEntry> saved, IReadOnlyList<BusBlock> draft) =>
            BusBlocks.Rows(saved, draft, bus => BusTags.MatchedTag(bus, Rules));

        [Fact]
        public void Split_RunsOfTheSameBus_CaseInsensitive()
        {
            List<BusBlock> blocks = BusBlocks.Split([E("A1", "c1"), E("a1", "c2"), E("A2", "c3"), E(null, "c4"), E(" ", "c5"), E("A1", "c6")]);
            Assert.Equal([("A1", 2), ("A2", 1), (null, 2), ("A1", 1)], blocks.Select(b => (b.Bus, b.Entries.Count)));
        }

        [Fact]
        public void Move_DownAndUp_ReturnsNewPositions()
        {
            List<BusBlock> blocks = BusBlocks.Split([E("A", "a"), E("B", "b"), E("C", "c"), E("D", "d")]);
            (List<BusBlock> down, IReadOnlyList<int> movedDown) = BusBlocks.Move(blocks, [0], 3);
            Assert.Equal(["B", "C", "A", "D"], down.Select(b => b.Bus));
            Assert.Equal([2], movedDown);
            (List<BusBlock> up, IReadOnlyList<int> movedUp) = BusBlocks.Move(blocks, [3], 1);
            Assert.Equal(["A", "D", "B", "C"], up.Select(b => b.Bus));
            Assert.Equal([1], movedUp);
        }

        [Fact]
        public void Move_SeveralBlocksKeepTheirOrder()
        {
            List<BusBlock> blocks = BusBlocks.Split([E("A", "a"), E("B", "b"), E("C", "c"), E("D", "d"), E("E", "e")]);
            (List<BusBlock> moved, IReadOnlyList<int> at) = BusBlocks.Move(blocks, [3, 1], 5);
            Assert.Equal(["A", "C", "E", "B", "D"], moved.Select(b => b.Bus));
            Assert.Equal([3, 4], at);
        }

        [Fact]
        public void Rows_UnsortedBuses_RowsNowAndAfterSort()
        {
            List<ConsumerEntry> saved = [.. Enumerable.Range(1, 3).Select(i => E("ANC15", $"a{i}")), .. Enumerable.Range(1, 2).Select(i => E("ANC2", $"b{i}")), E("ANC1", "c1")];
            IReadOnlyList<BusBlockRow> rows = Rows(saved, BusBlocks.Sort(BusBlocks.Split(saved), Rules));
            Assert.Equal(
                [("ANC1", 6, 6, 1, 1, 1), ("ANC2", 4, 5, 2, 3, 2), ("ANC15", 1, 3, 4, 6, 3)],
                rows.Select(r => (r.Bus, r.From, r.To, r.NewFrom, r.NewTo, r.Count)));
            Assert.All(rows, r => Assert.True(r.Moves));
        }

        [Fact]
        public void Rows_SavedOrder_NothingMoves_TagsAndUnknownShown()
        {
            List<ConsumerEntry> saved = [E("A1", "c1"), E("A1jnc2", "c2"), E(null, "c3")];
            IReadOnlyList<BusBlockRow> rows = Rows(saved, BusBlocks.Split(saved));
            Assert.All(rows, r => Assert.False(r.Moves));
            Assert.Equal([(string?)null, "jnc", null], rows.Select(r => r.HandPlacedTag));
            Assert.Equal("Unknown", rows[2].Bus);
        }
    }
}
