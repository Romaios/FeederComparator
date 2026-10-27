using Dapper;
using FeederCopy.Core.Services.Data;
using MySqlConnector;

namespace FeederCopy.Tests.Services
{
    public sealed class DatabaseTests
    {
        [Fact]
        public void ScriptNames_FromEmbeddedScripts_InOrder()
        {
            Assert.NotEmpty(Database.ScriptNames);
            Assert.Equal(Database.ScriptNames.Order(StringComparer.Ordinal), Database.ScriptNames);
            Assert.Contains("2026-10-23-01-create-tables", Database.ScriptNames);
        }

        [Fact]
        public void Check_AllScriptsRun_Ready()
        {
            using var temp = new TempDatabase();
            DatabaseCheck check = temp.Db.Check();
            Assert.True(check.Ready);
            Assert.StartsWith("8.", check.Version);
        }

        [Fact]
        public void Check_EmptyDatabase_ListsEveryScript()
        {
            using var temp = new TempDatabase(runScripts: false);
            DatabaseCheck check = temp.Db.Check();
            Assert.False(check.Ready);
            Assert.Equal(Database.ScriptNames, check.MissingScripts);
        }

        [Fact]
        public void Scripts_CreateTablesWithCoordinateColumns()
        {
            using var temp = new TempDatabase();
            using MySqlConnection c = temp.Db.Open();
            List<string> tables = c.Query<string>("SELECT table_name FROM information_schema.tables WHERE table_schema = DATABASE()").ToList();
            Assert.Superset(new HashSet<string>(["feeders", "feederconsumers", "readings", "consumerinactivefrom", "distributionlines", "schemascripts"]),
                tables.Select(t => t.ToLowerInvariant()).ToHashSet());
            Assert.DoesNotContain("networklines", tables.Select(t => t.ToLowerInvariant()));
            List<string> columns = c.Query<string>(
                "SELECT column_name FROM information_schema.columns WHERE table_schema = DATABASE() AND LOWER(table_name) = 'feederconsumers'").ToList();
            Assert.Contains("X", columns);
            Assert.Contains("Y", columns);
        }

        [Fact]
        public void DistributionLinesScript_WorksWhenAlreadyRenamedByHand()
        {
            using var temp = new TempDatabase(runScripts: false);
            temp.RunScript("2026-10-23-01-create-tables");
            using (MySqlConnection c = temp.Db.Open())
                c.Execute("RENAME TABLE NetworkLines TO DistributionLines");
            temp.RunScript("2026-10-27-01-rename-networklines-to-distributionlines");
            using MySqlConnection check = temp.Db.Open();
            List<string> indexes = check.Query<string>(
                "SELECT DISTINCT index_name FROM information_schema.statistics WHERE table_schema = DATABASE() AND LOWER(table_name) = 'distributionlines'").ToList();
            Assert.Contains("IX_DistributionLines_FromNode", indexes);
            Assert.Contains("IX_DistributionLines_ToNode", indexes);
            Assert.True(temp.Db.Check().Ready);
        }

        [Fact]
        public void Open_IncompleteConnection_SaysWhereToSetIt()
        {
            var db = new Database(DatabaseConnection.Empty);
            var ex = Assert.Throws<InvalidOperationException>(() => db.Open());
            Assert.Equal("The database connection is not set. Enter it in Settings → Database.", ex.Message);
        }

        [Fact]
        public void Open_WrongPassword_Throws()
        {
            var db = new Database(TempDatabase.Server with { Password = "definitely-wrong-" + Guid.NewGuid() });
            Assert.ThrowsAny<MySqlException>(() => db.Open());
        }
    }
}
