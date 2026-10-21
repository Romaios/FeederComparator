using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Tests.Models
{
    public sealed class BusSorterTests
    {
        private static readonly IReadOnlyList<BusTagRule> Tags = BusTags.ParseRules("sp, jnc");

        private static ConsumerEntry E(string? bus, string id) => new(bus, id);

        private static IEnumerable<string?> SortBuses(params string[] buses) =>
            BusSorter.Sort(buses.Select((b, i) => E(b, $"c{i}")).ToList(), Tags).Select(e => e.BusId);

        [Fact]
        public void Sort_NumbersByValue() =>
            Assert.Equal(["A1", "A2", "A10"], SortBuses("A10", "A2", "A1"));

        [Fact]
        public void Sort_ParentFirst_DashChildrenBeforeLetters() =>
            Assert.Equal(["A3", "A3-1", "A3A", "A3B"], SortBuses("A3B", "A3-1", "A3", "A3A"));

        [Fact]
        public void Sort_SubBusesStayUnderParent() =>
            Assert.Equal(["AFSEC3-6", "AFSEC3-6A", "AFSEC3-7", "AFSEC4", "AFSEC35"],
                SortBuses("AFSEC3-7", "AFSEC3-6A", "AFSEC3-6", "AFSEC4", "AFSEC35"));

        [Fact]
        public void Sort_TaggedBusesStayWithBusAbove() =>
            Assert.Equal(["A3-1", "A3-1sp1", "A3-2", "A3-2jnc2", "A3-2jnc1"],
                SortBuses("A3-2", "A3-2jnc2", "A3-2jnc1", "A3-1", "A3-1sp1"));

        [Fact]
        public void Sort_TaggedBusAtTop_SortsByItsUntaggedPart() =>
            Assert.Equal(["A3", "A3jnc1", "A4"], SortBuses("A3jnc1", "A4", "A3"));

        [Fact]
        public void Sort_CaseInsensitive() =>
            Assert.Equal(["abc-1", "ABC-2", "abc-3"], SortBuses("abc-3", "ABC-2", "abc-1"));

        [Fact]
        public void Sort_KeepsConsumerOrderInsideBus_UnknownLast()
        {
            List<ConsumerEntry> sorted = BusSorter.Sort([E("A2", "c1"), E("A2", "c2"), E(null, "c3"), E("A1", "c4"), E("A2", "c5")], Tags);
            Assert.Equal([E("A1", "c4"), E("A2", "c1"), E("A2", "c2"), E("A2", "c5"), E(null, "c3")], sorted);
        }

        [Fact]
        public void IsHandPlaced_MatchesTagFollowedByNumber()
        {
            Assert.True(BusSorter.IsHandPlaced("BFSEC16-2sp1", Tags));
            Assert.True(BusSorter.IsHandPlaced("CFSEC26-4ASP1", Tags));
            Assert.True(BusSorter.IsHandPlaced("AFSEC3JNC2", Tags));
            Assert.False(BusSorter.IsHandPlaced("GPSEC5", Tags));
            Assert.False(BusSorter.IsHandPlaced("AFSEC3-6D", Tags));
        }

        [Fact]
        public void Sort_TaggedBusUnderUnrelatedBus_GoesUnderItsOwnParent() =>
            Assert.Equal(["A3", "A3jnc1", "A3jnc2", "A4"], SortBuses("A4", "A3jnc1", "A3jnc2", "A3"));

        [Fact]
        public void Sort_TaggedChainStaysWithParentFamily() =>
            Assert.Equal(["B16-2", "B16-2sp2", "B16-2sp2jnc1", "B16-2jnc1", "B16-3"],
                SortBuses("B16-3", "B16-2", "B16-2sp2", "B16-2sp2jnc1", "B16-2jnc1"));

        [Fact]
        public void MovedRows_CountsOnlyRowsOfBusesThatMove()
        {
            List<ConsumerEntry> before = [E("A2", "c1"), E("A1", "c2"), E("A3", "c3"), E("A4", "c4"), E("A4", "c5")];
            Assert.Equal(1, BusSorter.MovedRows(before, BusSorter.Sort(before, Tags)));
        }

        [Fact]
        public void MovedRows_AlreadySorted_Zero()
        {
            List<ConsumerEntry> before = [E("A1", "c1"), E("A2", "c2")];
            Assert.Equal(0, BusSorter.MovedRows(before, BusSorter.Sort(before, Tags)));
        }

        [Fact]
        public void MovedRows_SameBusInTwoPlaces_UnchangedOrderIsZero()
        {
            List<ConsumerEntry> before = [E("A5", "c1"), E("A5jnc1", "c2"), E("A5", "c3"), E("A2", "c4")];
            Assert.Equal(0, BusSorter.MovedRows(before, before.ToList()));
        }

        [Fact]
        public void MovedRows_BlockDraggedToTop_CountsItsRows()
        {
            List<ConsumerEntry> before = [E("A1", "c1"), E("A1", "c2"), E("A2", "c3"), E("A3", "c4"), E("A3", "c5")];
            List<ConsumerEntry> after = [E("A3", "c4"), E("A3", "c5"), E("A1", "c1"), E("A1", "c2"), E("A2", "c3")];
            Assert.Equal(2, BusSorter.MovedRows(before, after));
        }
    }
}
