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
