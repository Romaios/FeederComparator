using Dapper;
using Microsoft.Data.Sqlite;

namespace FeederCopy.Tests.Support
{
    internal sealed class OldDatabase : IDisposable
    {
        private const string Tables = """
            CREATE TABLE Feeders (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL UNIQUE COLLATE NOCASE,
                CreatedAt TEXT NOT NULL
            );
            CREATE TABLE FeederConsumers (
                Id INTEGER PRIMARY KEY,
                FeederId INTEGER NOT NULL REFERENCES Feeders(Id) ON DELETE CASCADE,
                BusId TEXT NULL,
                ConsumerId TEXT NOT NULL UNIQUE,
                Position INTEGER NOT NULL
            );
            CREATE INDEX IX_FeederConsumers_Feeder_Position ON FeederConsumers(FeederId, Position);
            """;

        private const string StatusAndReadings = """
            ALTER TABLE FeederConsumers ADD COLUMN Active INTEGER NOT NULL DEFAULT 1;
            CREATE TABLE Readings (
                ConsumerId TEXT NOT NULL,
                BillMonth TEXT NOT NULL,
                Kwh REAL NULL,
                Status TEXT NOT NULL,
                PRIMARY KEY (ConsumerId, BillMonth)
            );
            CREATE INDEX IX_Readings_BillMonth ON Readings(BillMonth);
            CREATE TABLE ConsumerInactiveFrom (
                ConsumerId TEXT NOT NULL PRIMARY KEY,
                BillMonth TEXT NOT NULL
            );
            """;

        public OldDatabase(bool withStatus = true)
        {
            Path = TestFiles.TempPath(".db");
            using SqliteConnection c = Open();
            c.Execute(Tables);
            if (withStatus)
                c.Execute(StatusAndReadings);
        }

        public string Path { get; }

        public void Execute(string sql, object? args = null)
        {
            using SqliteConnection c = Open();
            c.Execute(sql, args);
        }

        public void Dispose() => File.Delete(Path);

        private SqliteConnection Open()
        {
            var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path, Pooling = false }.ToString());
            c.Open();
            return c;
        }
    }
}
