using Dapper;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Services.Data;
using MySqlConnector;

namespace FeederCopy.Tests.Services
{
    public sealed class LegacyImporterTests : IDisposable
    {
        private readonly TempDatabase _temp = new();
        private readonly OldDatabase _old = new();
        private readonly LegacyImporter _importer;
        private readonly FeederService _feeders;
        private readonly ReadingService _readings;

        public LegacyImporterTests()
        {
            _importer = new LegacyImporter(_temp.Db);
            _feeders = new FeederService(_temp.Db);
            _readings = new ReadingService(_temp.Db);
        }

        public void Dispose()
        {
            _temp.Dispose();
            _old.Dispose();
        }

        private void SeedOld()
        {
            _old.Execute("""
                INSERT INTO Feeders (Id, Name, CreatedAt) VALUES (4, 'Washington', '2026-10-20 08:15:00'), (7, 'Brooklyn', '2026-10-21 13:40:05');
                INSERT INTO FeederConsumers (FeederId, BusId, ConsumerId, Position, Active) VALUES
                    (4, 'BUS-2', 'c3', 2, 1),
                    (4, 'BUS-1', 'c1', 0, 1),
                    (4, 'BUS-1', 'c2', 1, 0),
                    (7, NULL, 'c4', 0, 0);
                INSERT INTO ConsumerInactiveFrom (ConsumerId, BillMonth) VALUES ('c2', '202607');
                INSERT INTO Readings (ConsumerId, BillMonth, Kwh, Status) VALUES
                    ('c1', '202606', 12.5, 'Active'),
                    ('c2', '202606', 4, 'Active'),
                    ('c1', '202607', 10, 'Active'),
                    ('c2', '202607', NULL, 'Inactive'),
                    ('c4', '202607', NULL, 'Missing reading');
                """);
        }

        [Fact]
        public void Import_CopiesFeedersConsumersStatusesAndReadings()
        {
            SeedOld();
            LegacyImportResult result = _importer.Import(_old.Path);

            Assert.Equal(new LegacyImportResult(2, 4, 2, 2, 5), result);
            IReadOnlyList<FeederSummary> feeders = _feeders.GetFeeders();
            Assert.Equal([("Brooklyn", 1, 0), ("Washington", 3, 2)], feeders.Select(f => (f.Name, f.Consumers, f.Buses)));
            Assert.Equal(
                [new ConsumerEntry("BUS-1", "c1", true), new ConsumerEntry("BUS-1", "c2", false), new ConsumerEntry("BUS-2", "c3", true)],
                _feeders.GetConsumers(feeders[1].Id));
            Assert.Equal([new ConsumerEntry(null, "c4", false)], _feeders.GetConsumers(feeders[0].Id));
            Assert.Equal(new BillMonth(2026, 7), _feeders.GetInactiveFrom()["c2"]);
            Assert.Equal([new BillMonth(2026, 6), new BillMonth(2026, 7)], _readings.GetMonths());
            Assert.Equal(
                [new ReadingRecord("c1", "202606", 12.5, "Active"), new ReadingRecord("c1", "202607", 10, "Active"), new ReadingRecord("c2", "202606", 4, "Active"),
                 new ReadingRecord("c2", "202607", null, "Inactive"), new ReadingRecord("c4", "202607", null, "Missing reading")],
                _readings.GetYear(2026).OrderBy(r => r.ConsumerId).ThenBy(r => r.BillMonth));
            using MySqlConnection c = _temp.Db.Open();
            Assert.Equal(new DateTime(2026, 10, 20, 8, 15, 0), c.ExecuteScalar<DateTime>("SELECT CreatedAt FROM Feeders WHERE Name = 'Washington'"));
        }

        [Fact]
        public void Import_LeavesTheOldFileUnchanged()
        {
            SeedOld();
            byte[] before = File.ReadAllBytes(_old.Path);
            _importer.Import(_old.Path);
            Assert.Equal(before, File.ReadAllBytes(_old.Path));
        }

        [Fact]
        public void Import_DatabaseAlreadyHasFeeders_RefusesAndAddsNothing()
        {
            SeedOld();
            _feeders.CreateFeeder("Queens", [new ConsumerEntry("A", "c9")]);
            var ex = Assert.Throws<LegacyImportException>(() => _importer.Import(_old.Path));
            Assert.Equal($"Database {_temp.Name} already has 1 feeder. The old data can only be copied into an empty database.", ex.Message);
            Assert.Equal(["Queens"], _feeders.GetFeeders().Select(f => f.Name));
            Assert.Empty(_readings.GetMonths());
        }

        [Fact]
        public void Import_FailsPartWay_SavesNothing()
        {
            SeedOld();
            _old.Execute("INSERT INTO Readings (ConsumerId, BillMonth, Kwh, Status) VALUES ('c3', '2026071', 1, 'Active')");
            Assert.ThrowsAny<MySqlException>(() => _importer.Import(_old.Path));
            Assert.Empty(_feeders.GetFeeders());
            Assert.Empty(_feeders.GetAllConsumers());
            Assert.Empty(_readings.GetMonths());
        }

        [Fact]
        public void Import_OldestFormat_WithoutStatusOrReadings_CopiesConsumersAsActive()
        {
            using var oldest = new OldDatabase(withStatus: false);
            oldest.Execute("""
                INSERT INTO Feeders (Id, Name, CreatedAt) VALUES (1, 'Washington', '2026-10-20 08:15:00');
                INSERT INTO FeederConsumers (FeederId, BusId, ConsumerId, Position) VALUES (1, 'A', 'c1', 0), (1, 'A', 'c2', 1);
                """);
            Assert.Equal(new LegacyImportResult(1, 2, 0, 0, 0), _importer.Import(oldest.Path));
            Assert.All(_feeders.GetAllConsumers(), c => Assert.True(c.Entry.Active));
        }

        [Fact]
        public void Import_OtherSqliteFile_SaysItIsNotAnOldDatabase()
        {
            string path = TestFiles.TempPath(".db");
            try
            {
                using (var c = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
                {
                    c.Open();
                    c.Execute("CREATE TABLE Something (Id INTEGER)");
                }
                var ex = Assert.Throws<LegacyImportException>(() => _importer.Import(path));
                Assert.StartsWith($"{Path.GetFileName(path)} is not a database from Feeder kWh Copy 1.x.", ex.Message);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Import_NotADatabaseAtAll_SaysItCouldNotBeRead()
        {
            string path = TestFiles.WriteRawText("these are not the rows you are looking for", ".db");
            try
            {
                var ex = Assert.Throws<LegacyImportException>(() => _importer.Import(path));
                Assert.StartsWith($"{Path.GetFileName(path)} could not be read as an old Feeder kWh Copy database.", ex.Message);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Import_MissingFile_SaysSo()
        {
            var ex = Assert.Throws<LegacyImportException>(() => _importer.Import(@"C:\nowhere\feeders.db"));
            Assert.Equal(@"The file was not found: C:\nowhere\feeders.db", ex.Message);
        }
    }
}
