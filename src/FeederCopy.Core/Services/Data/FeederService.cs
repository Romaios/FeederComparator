using Dapper;
using FeederCopy.Core.Models.Feeders;
using Microsoft.Data.Sqlite;

namespace FeederCopy.Core.Services.Data
{
    public sealed class FeederService : IFeederService
    {
        private const int LookupChunk = 500;
        private readonly Database _database;

        public FeederService(Database database) => _database = database;

        public IReadOnlyList<FeederSummary> GetFeeders()
        {
            using SqliteConnection c = _database.Open();
            return c.Query<(long Id, string Name, long Consumers, long Buses)>(
                    """
                    SELECT f.Id, f.Name,
                           COUNT(fc.Id),
                           COUNT(DISTINCT LOWER(fc.BusId))
                    FROM Feeders f
                    LEFT JOIN FeederConsumers fc ON fc.FeederId = f.Id
                    GROUP BY f.Id, f.Name
                    ORDER BY f.Name COLLATE NOCASE
                    """)
                .Select(r => new FeederSummary(r.Id, r.Name, (int)r.Consumers, (int)r.Buses))
                .ToList();
        }

        public IReadOnlyList<ConsumerEntry> GetConsumers(long feederId)
        {
            using SqliteConnection c = _database.Open();
            return c.Query<(string? BusId, string ConsumerId)>(
                    "SELECT BusId, ConsumerId FROM FeederConsumers WHERE FeederId = @feederId ORDER BY Position",
                    new { feederId })
                .Select(r => new ConsumerEntry(r.BusId, r.ConsumerId))
                .ToList();
        }

        public ConsumerLocation? FindConsumer(string consumerId) =>
            FindConsumers([consumerId]).Select(f => f.Where).FirstOrDefault();

        public IReadOnlyList<(string ConsumerId, ConsumerLocation Where)> FindConsumers(IEnumerable<string> consumerIds)
        {
            using SqliteConnection c = _database.Open();
            var found = new List<(string, ConsumerLocation)>();
            foreach (string[] chunk in consumerIds.Distinct(StringComparer.Ordinal).Chunk(LookupChunk))
            {
                found.AddRange(c.Query<(string ConsumerId, long FeederId, string Name, string? BusId)>(
                        """
                        SELECT fc.ConsumerId, f.Id, f.Name, fc.BusId
                        FROM FeederConsumers fc
                        JOIN Feeders f ON f.Id = fc.FeederId
                        WHERE fc.ConsumerId IN @chunk
                        """,
                        new { chunk })
                    .Select(r => (r.ConsumerId, new ConsumerLocation(r.FeederId, r.Name, r.BusId))));
            }
            return found;
        }

        public long CreateFeeder(string name, IReadOnlyList<ConsumerEntry> consumers)
        {
            using SqliteConnection c = _database.Open();
            using SqliteTransaction tx = c.BeginTransaction();
            long id = c.ExecuteScalar<long>(
                "INSERT INTO Feeders (Name, CreatedAt) VALUES (@name, @createdAt); SELECT last_insert_rowid();",
                new { name, createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                tx);
            InsertConsumers(c, tx, id, consumers);
            tx.Commit();
            return id;
        }

        public void SaveConsumers(IReadOnlyDictionary<long, IReadOnlyList<ConsumerEntry>> byFeeder)
        {
            using SqliteConnection c = _database.Open();
            using SqliteTransaction tx = c.BeginTransaction();
            foreach (long feederId in byFeeder.Keys)
                c.Execute("DELETE FROM FeederConsumers WHERE FeederId = @feederId", new { feederId }, tx);
            foreach ((long feederId, IReadOnlyList<ConsumerEntry> consumers) in byFeeder)
                InsertConsumers(c, tx, feederId, consumers);
            tx.Commit();
        }

        public IReadOnlyList<long> ImportFeeders(IReadOnlyList<(string Name, IReadOnlyList<ConsumerEntry> Consumers)> feeders)
        {
            using SqliteConnection c = _database.Open();
            using SqliteTransaction tx = c.BeginTransaction();
            var ids = new List<long>();
            foreach ((string name, _) in feeders)
            {
                long? existing = c.ExecuteScalar<long?>("SELECT Id FROM Feeders WHERE Name = @name", new { name }, tx);
                long id = existing ?? c.ExecuteScalar<long>(
                    "INSERT INTO Feeders (Name, CreatedAt) VALUES (@name, @createdAt); SELECT last_insert_rowid();",
                    new { name, createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                    tx);
                c.Execute("DELETE FROM FeederConsumers WHERE FeederId = @id", new { id }, tx);
                ids.Add(id);
            }
            for (int i = 0; i < feeders.Count; i++)
                InsertConsumers(c, tx, ids[i], feeders[i].Consumers);
            tx.Commit();
            return ids;
        }

        public void RenameFeeder(long feederId, string name)
        {
            using SqliteConnection c = _database.Open();
            c.Execute("UPDATE Feeders SET Name = @name WHERE Id = @feederId", new { name, feederId });
        }

        public void DeleteFeeder(long feederId)
        {
            using SqliteConnection c = _database.Open();
            c.Execute("DELETE FROM Feeders WHERE Id = @feederId", new { feederId });
        }

        private static void InsertConsumers(SqliteConnection c, SqliteTransaction tx, long feederId, IReadOnlyList<ConsumerEntry> consumers)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO FeederConsumers (FeederId, BusId, ConsumerId, Position) VALUES ($feeder, $bus, $consumer, $position)";
            SqliteParameter feeder = cmd.Parameters.Add("$feeder", SqliteType.Integer);
            SqliteParameter bus = cmd.Parameters.Add("$bus", SqliteType.Text);
            SqliteParameter consumer = cmd.Parameters.Add("$consumer", SqliteType.Text);
            SqliteParameter position = cmd.Parameters.Add("$position", SqliteType.Integer);
            cmd.Prepare();

            feeder.Value = feederId;
            for (int i = 0; i < consumers.Count; i++)
            {
                bus.Value = (object?)consumers[i].BusId ?? DBNull.Value;
                consumer.Value = consumers[i].ConsumerId;
                position.Value = i + 1;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
