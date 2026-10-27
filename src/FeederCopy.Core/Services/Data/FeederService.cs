using Dapper;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Feeders;
using MySqlConnector;

namespace FeederCopy.Core.Services.Data
{
    public sealed class FeederService : IFeederService
    {
        private const int LookupChunk = 500;
        private const string StatusTable = "TmpConsumerStatus";
        private const string LayoutTable = "TmpConsumerLayout";
        private readonly Database _database;

        public FeederService(Database database) => _database = database;

        public IReadOnlyList<FeederSummary> GetFeeders()
        {
            using MySqlConnection c = _database.Open();
            return c.Query<(long Id, string Name, long Consumers, long Buses)>(
                    """
                    SELECT f.Id, f.Name,
                           COALESCE(c.Consumers, 0),
                           COALESCE(b.Buses, 0)
                    FROM Feeders f
                    LEFT JOIN (
                        SELECT FeederId, COUNT(*) AS Consumers
                        FROM FeederConsumers
                        GROUP BY FeederId
                    ) c ON c.FeederId = f.Id
                    LEFT JOIN (
                        SELECT FeederId, COUNT(*) AS Buses
                        FROM (SELECT DISTINCT FeederId, LOWER(BusId) AS Bus FROM FeederConsumers WHERE BusId IS NOT NULL) d
                        GROUP BY FeederId
                    ) b ON b.FeederId = f.Id
                    ORDER BY f.Name
                    """)
                .Select(r => new FeederSummary(r.Id, r.Name, (int)r.Consumers, (int)r.Buses))
                .ToList();
        }

        public IReadOnlyList<ConsumerEntry> GetConsumers(long feederId)
        {
            using MySqlConnection c = _database.Open();
            return c.Query<(string? BusId, string ConsumerId, bool Active)>(
                    "SELECT BusId, ConsumerId, Active FROM FeederConsumers WHERE FeederId = @feederId ORDER BY Position",
                    new { feederId })
                .Select(r => new ConsumerEntry(r.BusId, r.ConsumerId, r.Active))
                .ToList();
        }

        public IReadOnlyList<(string FeederName, ConsumerEntry Entry)> GetAllConsumers()
        {
            using MySqlConnection c = _database.Open();
            return c.Query<(string Name, string? BusId, string ConsumerId, bool Active)>(
                    """
                    SELECT f.Name, fc.BusId, fc.ConsumerId, fc.Active
                    FROM FeederConsumers fc
                    JOIN Feeders f ON f.Id = fc.FeederId
                    ORDER BY f.Name, fc.Position
                    """)
                .Select(r => (r.Name, new ConsumerEntry(r.BusId, r.ConsumerId, r.Active)))
                .ToList();
        }

        public void SetActive(string consumerId, bool active, BillMonth? inactiveFrom = null) =>
            SetActive([consumerId], active, inactiveFrom);

        public void SetActive(IReadOnlyList<string> consumerIds, bool active, BillMonth? inactiveFrom = null)
        {
            using MySqlConnection c = _database.Open();
            using MySqlTransaction tx = c.BeginTransaction();
            MySqlBulk.FillIdTable(c, tx, StatusTable, consumerIds.Select(id => (id, active)));
            c.Execute($"UPDATE FeederConsumers fc JOIN {StatusTable} s ON s.ConsumerId = fc.ConsumerId SET fc.Active = s.Active", transaction: tx);
            if (!active && inactiveFrom is { } month)
                c.Execute(
                    $"""
                    INSERT INTO ConsumerInactiveFrom (ConsumerId, BillMonth)
                    SELECT s.ConsumerId, @month FROM {StatusTable} s
                    ON DUPLICATE KEY UPDATE BillMonth = @month
                    """,
                    new { month = month.Key }, tx);
            else
                c.Execute($"DELETE i FROM ConsumerInactiveFrom i JOIN {StatusTable} s ON s.ConsumerId = i.ConsumerId", transaction: tx);
            MySqlBulk.DropTable(c, tx, StatusTable);
            tx.Commit();
        }

        public void ApplyMonthStatus(IReadOnlyList<(string ConsumerId, bool Active)> statuses, BillMonth month)
        {
            using MySqlConnection c = _database.Open();
            using MySqlTransaction tx = c.BeginTransaction();
            MySqlBulk.FillIdTable(c, tx, StatusTable, statuses);
            var args = new { month = month.Key };
            c.Execute(
                $"""
                INSERT INTO ConsumerInactiveFrom (ConsumerId, BillMonth)
                SELECT s.ConsumerId, @month FROM {StatusTable} s
                JOIN FeederConsumers fc ON fc.ConsumerId = s.ConsumerId
                WHERE s.Active = FALSE AND fc.Active = TRUE
                ON DUPLICATE KEY UPDATE BillMonth = @month
                """,
                args, tx);
            c.Execute($"INSERT IGNORE INTO ConsumerInactiveFrom (ConsumerId, BillMonth) SELECT s.ConsumerId, @month FROM {StatusTable} s WHERE s.Active = FALSE", args, tx);
            c.Execute($"DELETE i FROM ConsumerInactiveFrom i JOIN {StatusTable} s ON s.ConsumerId = i.ConsumerId WHERE s.Active = TRUE", transaction: tx);
            c.Execute($"UPDATE FeederConsumers fc JOIN {StatusTable} s ON s.ConsumerId = fc.ConsumerId SET fc.Active = s.Active", transaction: tx);
            MySqlBulk.DropTable(c, tx, StatusTable);
            tx.Commit();
        }

        public IReadOnlyDictionary<string, BillMonth> GetInactiveFrom()
        {
            using MySqlConnection c = _database.Open();
            return c.Query<(string ConsumerId, string BillMonth)>(
                    """
                    SELECT i.ConsumerId, i.BillMonth
                    FROM ConsumerInactiveFrom i
                    JOIN FeederConsumers fc ON fc.ConsumerId = i.ConsumerId
                    WHERE fc.Active = FALSE
                    """)
                .ToDictionary(r => r.ConsumerId, r => BillMonth.Parse(r.BillMonth), StringComparer.Ordinal);
        }

        public ConsumerLocation? FindConsumer(string consumerId) =>
            FindConsumers([consumerId]).Select(f => f.Where).FirstOrDefault();

        public IReadOnlyList<(string ConsumerId, ConsumerLocation Where)> FindConsumers(IEnumerable<string> consumerIds)
        {
            using MySqlConnection c = _database.Open();
            return Find(c, null, consumerIds);
        }

        public long CreateFeeder(string name, IReadOnlyList<ConsumerEntry> consumers)
        {
            using MySqlConnection c = _database.Open();
            using MySqlTransaction tx = c.BeginTransaction();
            long id = InsertFeeder(c, tx, name);
            WriteConsumers(c, tx, new Dictionary<long, IReadOnlyList<ConsumerEntry>> { [id] = consumers });
            tx.Commit();
            return id;
        }

        public void SaveConsumers(IReadOnlyDictionary<long, IReadOnlyList<ConsumerEntry>> byFeeder)
        {
            using MySqlConnection c = _database.Open();
            using MySqlTransaction tx = c.BeginTransaction();
            WriteConsumers(c, tx, byFeeder);
            tx.Commit();
        }

        public IReadOnlyList<long> ImportFeeders(IReadOnlyList<(string Name, IReadOnlyList<ConsumerEntry> Consumers)> feeders)
        {
            using MySqlConnection c = _database.Open();
            using MySqlTransaction tx = c.BeginTransaction();
            var ids = new List<long>();
            foreach ((string name, _) in feeders)
            {
                long? existing = c.ExecuteScalar<long?>("SELECT Id FROM Feeders WHERE Name = @name", new { name }, tx);
                ids.Add(existing ?? InsertFeeder(c, tx, name));
            }
            var byFeeder = new Dictionary<long, IReadOnlyList<ConsumerEntry>>();
            for (int i = 0; i < feeders.Count; i++)
                byFeeder[ids[i]] = feeders[i].Consumers;
            WriteConsumers(c, tx, byFeeder);
            tx.Commit();
            return ids;
        }

        public void RenameFeeder(long feederId, string name)
        {
            using MySqlConnection c = _database.Open();
            c.Execute("UPDATE Feeders SET Name = @name WHERE Id = @feederId", new { name, feederId });
        }

        public void DeleteFeeder(long feederId)
        {
            using MySqlConnection c = _database.Open();
            c.Execute("DELETE FROM Feeders WHERE Id = @feederId", new { feederId });
        }

        private static long InsertFeeder(MySqlConnection c, MySqlTransaction tx, string name) =>
            c.ExecuteScalar<long>(
                "INSERT INTO Feeders (Name, CreatedAt) VALUES (@name, @createdAt); SELECT LAST_INSERT_ID();",
                new { name, createdAt = DateTime.Now },
                tx);

        private static List<(string ConsumerId, ConsumerLocation Where)> Find(MySqlConnection c, MySqlTransaction? tx, IEnumerable<string> consumerIds)
        {
            var found = new List<(string, ConsumerLocation)>();
            foreach (string[] chunk in consumerIds.Distinct(StringComparer.Ordinal).Chunk(LookupChunk))
            {
                found.AddRange(c.Query<(string ConsumerId, long FeederId, string Name, string? BusId)>(
                        """
                        SELECT STRAIGHT_JOIN fc.ConsumerId, f.Id, f.Name, fc.BusId
                        FROM FeederConsumers fc
                        JOIN Feeders f ON f.Id = fc.FeederId
                        WHERE fc.ConsumerId IN @chunk
                        """,
                        new { chunk },
                        tx)
                    .Select(r => (r.ConsumerId, new ConsumerLocation(r.FeederId, r.Name, r.BusId))));
            }
            return found;
        }

        private static void WriteConsumers(MySqlConnection c, MySqlTransaction tx, IReadOnlyDictionary<long, IReadOnlyList<ConsumerEntry>> byFeeder)
        {
            var incoming = byFeeder.SelectMany(p => p.Value.Select(e => e.ConsumerId)).ToHashSet(StringComparer.Ordinal);
            (string ConsumerId, ConsumerLocation Where) elsewhere = Find(c, tx, incoming).FirstOrDefault(f => !byFeeder.ContainsKey(f.Where.FeederId));
            if (elsewhere.ConsumerId is not null)
                throw new InvalidOperationException($"Consumer {elsewhere.ConsumerId} is already in feeder {elsewhere.Where.FeederName}.");

            List<string> removed = byFeeder.Keys
                .SelectMany(feederId => c.Query<string>("SELECT ConsumerId FROM FeederConsumers WHERE FeederId = @feederId", new { feederId }, tx))
                .Where(id => !incoming.Contains(id))
                .ToList();

            List<object?[]> rows = byFeeder
                .SelectMany(p => p.Value.Select((e, i) => new object?[] { p.Key, e.BusId, e.ConsumerId, i + 1, e.Active }))
                .ToList();
            c.Execute($"DROP TEMPORARY TABLE IF EXISTS {LayoutTable}", transaction: tx);
            c.Execute(
                $"""
                CREATE TEMPORARY TABLE {LayoutTable} (
                    FeederId BIGINT NOT NULL,
                    BusId VARCHAR(100) NULL,
                    ConsumerId VARCHAR(50) NOT NULL PRIMARY KEY,
                    Position INT NOT NULL,
                    Active BOOLEAN NOT NULL)
                """,
                transaction: tx);
            MySqlBulk.Insert(c, tx, $"INSERT INTO {LayoutTable} (FeederId, BusId, ConsumerId, Position, Active)", rows);
            c.Execute(
                $"""
                UPDATE FeederConsumers fc JOIN {LayoutTable} l ON l.ConsumerId = fc.ConsumerId
                SET fc.FeederId = l.FeederId, fc.BusId = l.BusId, fc.Position = l.Position, fc.Active = l.Active
                """,
                transaction: tx);
            c.Execute(
                $"""
                INSERT INTO FeederConsumers (FeederId, BusId, ConsumerId, Position, Active)
                SELECT l.FeederId, l.BusId, l.ConsumerId, l.Position, l.Active FROM {LayoutTable} l
                LEFT JOIN FeederConsumers fc ON fc.ConsumerId = l.ConsumerId
                WHERE fc.Id IS NULL
                """,
                transaction: tx);
            c.Execute($"DROP TEMPORARY TABLE IF EXISTS {LayoutTable}", transaction: tx);

            foreach (string[] chunk in removed.Chunk(LookupChunk))
                c.Execute("DELETE FROM FeederConsumers WHERE ConsumerId IN @chunk", new { chunk }, tx);
        }
    }
}
