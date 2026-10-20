using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Tests.Models
{
    public sealed class FeederLayoutTests
    {
        private static ConsumerEntry E(string? bus, string id) => new(bus, id);

        [Fact]
        public void Arrange_GroupsInterleavedBusesByFirstAppearance() =>
            Assert.Equal([E("A", "c1"), E("A", "c3"), E("B", "c2")],
                FeederLayout.Arrange([E("A", "c1"), E("B", "c2"), E("A", "c3")]));

        [Fact]
        public void Arrange_UnknownAlwaysLast() =>
            Assert.Equal([E("A", "c2"), E(null, "c1")], FeederLayout.Arrange([E(null, "c1"), E("A", "c2")]));

        [Fact]
        public void Arrange_KeepsFirstSpelling() =>
            Assert.Equal([E("CONSjunc14Se", "c1"), E("CONSjunc14Se", "c2")],
                FeederLayout.Arrange([E("CONSjunc14Se", "c1"), E("consjunc14se", "c2")]));

        [Fact]
        public void Add_ExistingBus_InsertsAfterLastOfThatBus() =>
            Assert.Equal([E("A", "c1"), E("A", "c2"), E("A", "c9"), E("B", "c3")],
                FeederLayout.Add([E("A", "c1"), E("A", "c2"), E("B", "c3")], "A", "c9"));

        [Fact]
        public void Add_NewBus_GoesAboveUnknown() =>
            Assert.Equal([E("A", "c1"), E("Z", "c9"), E(null, "c2")],
                FeederLayout.Add([E("A", "c1"), E(null, "c2")], "Z", "c9"));

        [Fact]
        public void Add_NewBusInFamily_GoesAfterLowerSibling() =>
            Assert.Equal([E("ABCjunc1", "c1"), E("ABCjunc2", "c2"), E("abcjunc3", "c9"), E("XYZ", "c3")],
                FeederLayout.Add([E("ABCjunc1", "c1"), E("ABCjunc2", "c2"), E("XYZ", "c3")], "abcjunc3", "c9"));

        [Fact]
        public void Add_NewBusInDashFamily_GoesAfterLowerSibling() =>
            Assert.Equal([E("ABC-1", "c1"), E("ABC-2", "c2"), E("abc-3", "c9"), E("Other", "c3")],
                FeederLayout.Add([E("ABC-1", "c1"), E("ABC-2", "c2"), E("Other", "c3")], "abc-3", "c9"));

        [Fact]
        public void Add_NewBusInFamily_GoesBetweenSiblings() =>
            Assert.Equal([E("J1", "c1"), E("J2", "c2"), E("J3", "c9"), E("J4", "c4"), E("Z", "c5")],
                FeederLayout.Add([E("J1", "c1"), E("J2", "c2"), E("J4", "c4"), E("Z", "c5")], "J3", "c9"));

        [Fact]
        public void Add_NewBusLowerThanAllSiblings_GoesBeforeLowest() =>
            Assert.Equal([E("Z", "c0"), E("J1", "c9"), E("J2", "c1"), E("J3", "c2")],
                FeederLayout.Add([E("Z", "c0"), E("J2", "c1"), E("J3", "c2")], "J1", "c9"));

        [Fact]
        public void Add_NewBusInFamily_UsesHighestLowerSiblingWherever() =>
            Assert.Equal([E("J1", "c1"), E("Z", "c2"), E("J2", "c3"), E("J3", "c9")],
                FeederLayout.Add([E("J1", "c1"), E("Z", "c2"), E("J2", "c3")], "J3", "c9"));

        [Fact]
        public void Add_NewBusInFamily_StaysAboveUnknown() =>
            Assert.Equal([E("J1", "c1"), E("J2", "c9"), E(null, "c5")],
                FeederLayout.Add([E("J1", "c1"), E(null, "c5")], "J2", "c9"));

        [Fact]
        public void Add_NewBusDifferentPrefix_GoesToBottom() =>
            Assert.Equal([E("ABC-1", "c1"), E("Z", "c2"), E("ABC2", "c9")],
                FeederLayout.Add([E("ABC-1", "c1"), E("Z", "c2")], "ABC2", "c9"));

        [Fact]
        public void Add_NewBusNumberWithLeadingZeros_ComparesByValue() =>
            Assert.Equal([E("J1", "c1"), E("J02", "c9"), E("J10", "c2")],
                FeederLayout.Add([E("J1", "c1"), E("J10", "c2")], "J02", "c9"));

        [Fact]
        public void SetBus_ToNewFamilyBus_MovesNextToFamily() =>
            Assert.Equal([E("J1", "c1"), E("J2", "c3"), E("Z", "c2")],
                FeederLayout.SetBus([E("J1", "c1"), E("Z", "c2"), E(null, "c3")], "c3", "J2"));

        [Fact]
        public void Add_BlankBus_GoesToUnknownAtBottom()
        {
            List<ConsumerEntry> result = FeederLayout.Add([E("A", "c1"), E("B", "c2")], "  ", "c9");
            Assert.Equal(E(null, "c9"), result[^1]);
        }

        [Fact]
        public void Add_TrimsConsumerAndBus() =>
            Assert.Equal([E("A", "c1"), E("A", "c9")], FeederLayout.Add([E("A", "c1")], " A ", " c9 "));

        [Fact]
        public void SetBus_UnknownToExisting_MovesIntoBlock() =>
            Assert.Equal([E("A", "c1"), E("A", "c3"), E("B", "c2")],
                FeederLayout.SetBus([E("A", "c1"), E("B", "c2"), E(null, "c3")], "c3", "a"));

        [Fact]
        public void Remove_DropsOnlyThatConsumer() =>
            Assert.Equal([E("A", "c1"), E("B", "c3")],
                FeederLayout.Remove([E("A", "c1"), E("A", "c2"), E("B", "c3")], "c2"));

        [Fact]
        public void ToRows_NumbersFromOne_UnknownLabel()
        {
            IReadOnlyList<ConsumerRow> rows = FeederLayout.ToRows([E("A", "c1"), E("A", "c2"), E(null, "c3")]);
            Assert.Equal([1, 2, 3], rows.Select(r => r.Count));
            Assert.Equal(new ConsumerRow(3, "Unknown", "c3"), rows[2]);
        }

        [Fact]
        public void SetBus_KeepsInactive() =>
            Assert.Equal([new ConsumerEntry("A", "c1"), new ConsumerEntry("A", "c2", false)],
                FeederLayout.SetBus([E("A", "c1"), new ConsumerEntry(null, "c2", false)], "c2", "A"));

        [Fact]
        public void AddEntry_KeepsInactive() =>
            Assert.Equal([E("A", "c1"), new ConsumerEntry("A", "c9", false)],
                FeederLayout.Add([E("A", "c1")], new ConsumerEntry(" A ", " c9 ", false)));

        [Fact]
        public void ToRows_CarriesStatus()
        {
            IReadOnlyList<ConsumerRow> rows = FeederLayout.ToRows([E("A", "c1"), new ConsumerEntry("A", "c2", false)]);
            Assert.Equal(["Active", "Inactive"], rows.Select(r => r.StatusLabel));
            Assert.False(rows[1].Active);
        }

        [Fact]
        public void Buses_DistinctInBlockOrder() =>
            Assert.Equal(["B", "A"], FeederLayout.Buses([E("B", "c1"), E("B", "c2"), E("A", "c3"), E(null, "c4")]));

        [Fact]
        public void NormalizeBus_BlankIsNull()
        {
            Assert.Null(FeederLayout.NormalizeBus("   "));
            Assert.Null(FeederLayout.NormalizeBus(null));
            Assert.Equal("A", FeederLayout.NormalizeBus(" A "));
        }
    }
}
