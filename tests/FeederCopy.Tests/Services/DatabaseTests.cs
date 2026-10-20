using Dapper;
using FeederCopy.Core.Services.Data;
using Microsoft.Data.Sqlite;

namespace FeederCopy.Tests.Services
{
    public sealed class DatabaseTests
    {
        [Fact]
        public void EnsureSchema_NewFile_CreatesTablesAndSetsUserVersion1()
        {
            using var temp = new TempDatabase();
            using SqliteConnection c = temp.Db.Open();
            List<string> tables = c.Query<string>("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name").ToList();
            Assert.Contains("Feeders", tables);
            Assert.Contains("FeederConsumers", tables);
            Assert.Equal(1L, c.ExecuteScalar<long>("PRAGMA user_version"));
        }

        [Fact]
        public void EnsureSchema_RunTwice_IsNoOp()
        {
            using var temp = new TempDatabase();
            temp.Db.EnsureSchema();
            using SqliteConnection c = temp.Db.Open();
            Assert.Equal(1L, c.ExecuteScalar<long>("PRAGMA user_version"));
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
