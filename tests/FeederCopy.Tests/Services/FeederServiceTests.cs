using Dapper;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Services.Data;
using Microsoft.Data.Sqlite;

namespace FeederCopy.Tests.Services
{
    public sealed class FeederServiceTests : IDisposable
    {
        private readonly TempDatabase _temp = new();
        private readonly FeederService _service;

        public FeederServiceTests() => _service = new FeederService(_temp.Db);

        public void Dispose() => _temp.Dispose();

        private static ConsumerEntry E(string? bus, string id) => new(bus, id);

        [Fact]
        public void CreateFeeder_ThenGetConsumers_KeepsOrderAndNullBus()
        {
            long id = _service.CreateFeeder("New York Feeder", [E("B", "c2"), E("A", "c1"), E(null, "c3")]);
            Assert.Equal([E("B", "c2"), E("A", "c1"), E(null, "c3")], _service.GetConsumers(id));
        }

        [Fact]
        public void GetFeeders_CountsConsumersAndDistinctBuses()
        {
            _service.CreateFeeder("Washington", [E("A", "c1"), E("A", "c2"), E("B", "c3"), E(null, "c4")]);
            _service.CreateFeeder("Brooklyn", []);
            IReadOnlyList<FeederSummary> feeders = _service.GetFeeders();
            Assert.Equal(["Brooklyn", "Washington"], feeders.Select(f => f.Name));
            Assert.Equal((0, 0), (feeders[0].Consumers, feeders[0].Buses));
            Assert.Equal((4, 2), (feeders[1].Consumers, feeders[1].Buses));
        }

        [Fact]
        public void FindConsumer_ReturnsFeederAndBus()
        {
            long id = _service.CreateFeeder("F", [E("A", "c1"), E(null, "c2")]);
            Assert.Equal(new ConsumerLocation(id, "F", "A"), _service.FindConsumer("c1"));
            Assert.Equal(new ConsumerLocation(id, "F", null), _service.FindConsumer("c2"));
            Assert.Null(_service.FindConsumer("c9"));
        }

        [Fact]
        public void FindConsumers_ManyIds()
        {
            List<ConsumerEntry> entries = Enumerable.Range(1, 1500).Select(i => E("A", $"c{i}")).ToList();
            long id = _service.CreateFeeder("F", entries);
            var found = _service.FindConsumers(entries.Select(e => e.ConsumerId).Append("zzz"));
            Assert.Equal(1500, found.Count);
            Assert.All(found, f => Assert.Equal(id, f.Where.FeederId));
        }

        [Fact]
        public void SaveConsumers_MoveBetweenFeeders_OneTransaction()
        {
            long f1 = _service.CreateFeeder("F1", [E("A", "c1"), E("A", "c2")]);
            long f2 = _service.CreateFeeder("F2", [E("B", "c3")]);
            _service.SaveConsumers(new Dictionary<long, IReadOnlyList<ConsumerEntry>>
            {
                [f2] = [E("B", "c3"), E("A", "c1")],
                [f1] = [E("A", "c2")],
            });
            Assert.Equal([E("A", "c2")], _service.GetConsumers(f1));
            Assert.Equal([E("B", "c3"), E("A", "c1")], _service.GetConsumers(f2));
        }

        [Fact]
        public void SaveConsumers_RollsBackOnFailure()
        {
            long f1 = _service.CreateFeeder("F1", [E("A", "c1")]);
            long f2 = _service.CreateFeeder("F2", [E("B", "c2")]);
            Assert.Throws<SqliteException>(() => _service.SaveConsumers(new Dictionary<long, IReadOnlyList<ConsumerEntry>>
            {
                [f1] = [E("A", "c9")],
                [999] = [E("B", "c8")],
            }));
            Assert.Equal([E("A", "c1")], _service.GetConsumers(f1));
            Assert.Equal([E("B", "c2")], _service.GetConsumers(f2));
        }

        [Fact]
        public void ImportFeeders_CreatesNewAndReplacesExistingByName()
        {
            long abuyog = _service.CreateFeeder("Abuyog", [E("A", "c1")]);
            IReadOnlyList<long> ids = _service.ImportFeeders([("abuyog", [E("A", "c1"), E("A", "c2")]), ("Bacon", [E("B", "c3")])]);
            Assert.Equal(abuyog, ids[0]);
            Assert.Equal(["Abuyog", "Bacon"], _service.GetFeeders().Select(f => f.Name));
            Assert.Equal([E("A", "c1"), E("A", "c2")], _service.GetConsumers(abuyog));
            Assert.Equal([E("B", "c3")], _service.GetConsumers(ids[1]));
        }

        [Fact]
        public void ImportFeeders_ConsumerMovedBetweenImportedFeeders_NoConflict()
        {
            _service.CreateFeeder("Abuyog", [E("A", "c1"), E("A", "c2")]);
            IReadOnlyList<long> ids = _service.ImportFeeders([("Bacon", [E("B", "c1")]), ("Abuyog", [E("A", "c2")])]);
            Assert.Equal([E("B", "c1")], _service.GetConsumers(ids[0]));
            Assert.Equal([E("A", "c2")], _service.GetConsumers(ids[1]));
        }

        [Fact]
        public void ImportFeeders_RollsBackOnFailure()
        {
            long other = _service.CreateFeeder("Other", [E("Z", "c9")]);
            long abuyog = _service.CreateFeeder("Abuyog", [E("A", "c1")]);
            Assert.Throws<SqliteException>(() => _service.ImportFeeders([("Abuyog", [E("A", "c2")]), ("Bacon", [E("B", "c9")])]));
            Assert.Equal([E("A", "c1")], _service.GetConsumers(abuyog));
            Assert.Equal([E("Z", "c9")], _service.GetConsumers(other));
            Assert.Equal(["Abuyog", "Other"], _service.GetFeeders().Select(f => f.Name));
        }

        [Fact]
        public void Consumers_ActiveFlagRoundTrips()
        {
            long id = _service.CreateFeeder("F", [E("A", "c1"), new ConsumerEntry("A", "c2", false)]);
            Assert.Equal([E("A", "c1"), new ConsumerEntry("A", "c2", false)], _service.GetConsumers(id));
        }

        [Fact]
        public void SetActive_UpdatesOnlyThatConsumer()
        {
            long id = _service.CreateFeeder("F", [E("A", "c1"), E("A", "c2")]);
            _service.SetActive("c2", false);
            Assert.Equal([E("A", "c1"), new ConsumerEntry("A", "c2", false)], _service.GetConsumers(id));
            _service.SetActive("c2", true);
            Assert.True(_service.GetConsumers(id)[1].Active);
        }

        [Fact]
        public void GetAllConsumers_FeederNameThenPosition()
        {
            _service.CreateFeeder("Bacon", [E("B", "c3")]);
            _service.CreateFeeder("Abuyog", [E("A", "c2"), new ConsumerEntry("A", "c1", false)]);
            var all = _service.GetAllConsumers();
            Assert.Equal([("Abuyog", "c2"), ("Abuyog", "c1"), ("Bacon", "c3")], all.Select(a => (a.FeederName, a.Entry.ConsumerId)));
            Assert.False(all[1].Entry.Active);
        }

        [Fact]
        public void DeleteFeeder_RemovesConsumers()
        {
            long id = _service.CreateFeeder("F", [E("A", "c1")]);
            _service.DeleteFeeder(id);
            Assert.Empty(_service.GetFeeders());
            Assert.Null(_service.FindConsumer("c1"));
        }

        [Fact]
        public void RenameFeeder_Persists()
        {
            long id = _service.CreateFeeder("F", []);
            _service.RenameFeeder(id, "Brooklyn Feeder");
            Assert.Equal("Brooklyn Feeder", _service.GetFeeders().Single().Name);
        }

        [Fact]
        public void CreatedAt_Format()
        {
            _service.CreateFeeder("F", []);
            using SqliteConnection c = _temp.Db.Open();
            string created = c.ExecuteScalar<string>("SELECT CreatedAt FROM Feeders")!;
            Assert.True(DateTime.TryParseExact(created, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out _));
        }
    }
}
