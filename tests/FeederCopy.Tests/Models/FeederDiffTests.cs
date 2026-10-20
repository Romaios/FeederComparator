using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Tests.Models
{
    public sealed class FeederDiffTests
    {
        private static ConsumerEntry E(string? bus, string id) => new(bus, id);

        [Fact]
        public void Compare_CountsAddedRemovedBusChanged() =>
            Assert.Equal(new FeederDiffResult(1, 1, 1),
                FeederDiff.Compare([E("A", "c1"), E("A", "c2"), E("B", "c3")], [E("A", "c1"), E("B", "c2"), E("B", "c4")]));

        [Fact]
        public void ToString_Text() =>
            Assert.Equal("1 added, 1 removed, 1 moved to a different bus", new FeederDiffResult(1, 1, 1).ToString());

        [Fact]
        public void Compare_BusCaseOnlyChange_NotCounted() =>
            Assert.Equal(new FeederDiffResult(0, 0, 0), FeederDiff.Compare([E("A", "c1")], [E("a", "c1")]));

        [Fact]
        public void Compare_UnknownToBus_CountsAsMoved() =>
            Assert.Equal(new FeederDiffResult(0, 0, 1), FeederDiff.Compare([E(null, "c1")], [E("A", "c1")]));
    }
}
