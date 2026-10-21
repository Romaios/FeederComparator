using Dapper;
using FeederCopy.Core.Services.Data;
using Microsoft.Data.Sqlite;

namespace FeederCopy.Tests.Services
{
    public sealed class DatabaseTests
    {
        [Fact]
        public void EnsureSchema_NewFile_CreatesTablesAndSetsUserVersion2()
        {
            using var temp = new TempDatabase();
            using SqliteConnection c = temp.Db.Open();
            List<string> tables = c.Query<string>("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name").ToList();
            Assert.Contains("Feeders", tables);
            Assert.Contains("FeederConsumers", tables);
            Assert.Contains("Readings", tables);
            Assert.Equal(3L, c.ExecuteScalar<long>("PRAGMA user_version"));
        }

        [Fact]
        public void EnsureSchema_RunTwice_IsNoOp()
        {
            using var temp = new TempDatabase();
            temp.Db.EnsureSchema();
            using SqliteConnection c = temp.Db.Open();
            Assert.Equal(3L, c.ExecuteScalar<long>("PRAGMA user_version"));
        }

        [Fact]
        public void EnsureSchema_UpgradeFromVersion1_KeepsConsumersAsActive()
        {
            string path = TestFiles.TempPath(".db");
            try
            {
                using (var c = new SqliteConnection($"Data Source={path}"))
                {
                    c.Open();
                    c.Execute(File.ReadAllText(Path.Combine(RepoRoot(), "docs", "sql", "2026-10-20-01-create-feeder-tables.sql")));
                    c.Execute("INSERT INTO Feeders (Id, Name, CreatedAt) VALUES (1, 'Abuyog', '2026-10-20 08:00:00')");
                    c.Execute("INSERT INTO FeederConsumers (FeederId, BusId, ConsumerId, Position) VALUES (1, 'A', '0900001', 1)");
                    c.Execute("PRAGMA user_version = 1");
                }
                TempDatabase.ClearPool(path);

                var db = new Database(path);
                db.EnsureSchema();
                using SqliteConnection upgraded = db.Open();
                Assert.Equal(3L, upgraded.ExecuteScalar<long>("PRAGMA user_version"));
                Assert.Equal(1L, upgraded.ExecuteScalar<long>("SELECT Active FROM FeederConsumers WHERE ConsumerId = '0900001'"));
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                File.Delete(path);
            }
        }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FeederCopy.sln")))
                dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
        }

        [Fact]
        public void EnsureSchema_NewerVersion_Throws()
        {
            using var temp = new TempDatabase();
            using (SqliteConnection c = temp.Db.Open())
                c.Execute("PRAGMA user_version = 5");
            var ex = Assert.Throws<InvalidOperationException>(() => new Database(temp.FilePath).EnsureSchema());
            Assert.Contains("newer version", ex.Message);
        }

        [Fact]
        public void Open_EnablesForeignKeys()
        {
            using var temp = new TempDatabase();
            using SqliteConnection c = temp.Db.Open();
            Assert.Equal(1L, c.ExecuteScalar<long>("PRAGMA foreign_keys"));
        }
    }
}
