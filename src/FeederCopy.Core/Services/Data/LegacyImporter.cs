using System.Globalization;
using Dapper;
using FeederCopy.Core.Models;
using Microsoft.Data.Sqlite;
using MySqlConnector;

namespace FeederCopy.Core.Services.Data
{
    public sealed record LegacyImportResult(int Feeders, int Consumers, int Inactive, int Months, int Readings);

    public sealed record LegacyTarget(string Database, int Feeders, int InactiveMonths, int Readings)
    {
        public bool HasLeftovers => InactiveMonths > 0 || Readings > 0;
    }

    public sealed class LegacyImportException(string message) : Exception(message);

    public interface ILegacyImporter
    {
        LegacyTarget Inspect();
        LegacyImportResult Import(string path, bool replaceLeftovers);
    }

    public sealed class LegacyImporter : ILegacyImporter
    {
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

        private readonly Database _database;

        public LegacyImporter(Database database) => _database = database;

        public LegacyTarget Inspect()
        {
            using MySqlConnection c = _database.Open();
            return Inspect(c, null);
        }

        public LegacyImportResult Import(string path, bool replaceLeftovers)
        {
            if (!File.Exists(path))
                throw new LegacyImportException($"The file was not found: {path}");
            OldData old = Read(path);

            using MySqlConnection c = _database.Open();
            using MySqlTransaction tx = c.BeginTransaction();
            LegacyTarget target = Inspect(c, tx);
            if (target.Feeders > 0)
                throw new LegacyImportException(HasFeeders(target));
            if (target.HasLeftovers)
            {
                if (!replaceLeftovers)
                    throw new LegacyImportException(Leftovers(target) + " Nothing was copied.");
                c.Execute("DELETE FROM Readings", transaction: tx);
                c.Execute("DELETE FROM ConsumerInactiveFrom", transaction: tx);
            }

            var feederIds = new Dictionary<long, long>();
            foreach (OldFeeder feeder in old.Feeders)
                feederIds[feeder.Id] = c.ExecuteScalar<long>(
                    "INSERT INTO Feeders (Name, CreatedAt) VALUES (@name, @createdAt); SELECT LAST_INSERT_ID();",
                    new { name = feeder.Name, createdAt = CreatedAt(feeder) }, tx);
            MySqlBulk.Insert(c, tx, "INSERT INTO FeederConsumers (FeederId, BusId, ConsumerId, Position, Active)",
                old.Consumers.Select(r => new object?[] { feederIds[r.FeederId], r.BusId, r.ConsumerId, r.Position, r.Active != 0 }).ToList());
            MySqlBulk.Insert(c, tx, "INSERT INTO ConsumerInactiveFrom (ConsumerId, BillMonth)",
                old.InactiveFrom.Select(r => new object?[] { r.ConsumerId, r.BillMonth }).ToList());
            MySqlBulk.Insert(c, tx, "INSERT INTO Readings (ConsumerId, BillMonth, Kwh, Status)",
                old.Readings.Select(r => new object?[] { r.ConsumerId, r.BillMonth, r.Kwh, r.Status }).ToList());
            tx.Commit();

            return new LegacyImportResult(
                old.Feeders.Count,
                old.Consumers.Count,
                old.Consumers.Count(r => r.Active == 0),
                old.Readings.Select(r => r.BillMonth).Distinct(StringComparer.Ordinal).Count(),
                old.Readings.Count);
        }

        public static string HasFeeders(LegacyTarget target) =>
            $"Database {target.Database} already has {Text.Count(target.Feeders, "feeder", "feeders")}. The old data can only be copied into a database with no feeders.";

        public static string Leftovers(LegacyTarget target) =>
            $"Database {target.Database} has no feeders, but still has {Text.Count(target.InactiveMonths, "inactive month", "inactive months")} and {Text.Count(target.Readings, "saved kWh reading", "saved kWh readings")} from before.";

        private LegacyTarget Inspect(MySqlConnection c, MySqlTransaction? tx) =>
            new(
                _database.Connection.Database,
                (int)c.ExecuteScalar<long>("SELECT COUNT(*) FROM Feeders", transaction: tx),
                (int)c.ExecuteScalar<long>("SELECT COUNT(*) FROM ConsumerInactiveFrom", transaction: tx),
                (int)c.ExecuteScalar<long>("SELECT COUNT(*) FROM Readings", transaction: tx));

        private static OldData Read(string path)
        {
            var builder = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false };
            using var c = new SqliteConnection(builder.ToString());
            try
            {
                c.Open();
                var tables = c.Query<string>("SELECT name FROM sqlite_master WHERE type = 'table'").ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (!tables.Contains("Feeders") || !tables.Contains("FeederConsumers"))
                    throw new LegacyImportException($"{Path.GetFileName(path)} is not a database from Feeder kWh Copy 1.x. Choose the feeders.db file from the old program folder.");
                bool hasActive = c.Query<string>("SELECT name FROM pragma_table_info('FeederConsumers')").Contains("Active", StringComparer.OrdinalIgnoreCase);

                List<OldFeeder> feeders = c.Query<OldFeeder>("SELECT Id, Name, CreatedAt FROM Feeders ORDER BY Id").ToList();
                List<OldConsumer> consumers = c.Query<OldConsumer>(
                    $"""
                    SELECT fc.FeederId, fc.BusId, fc.ConsumerId, fc.Position, {(hasActive ? "fc.Active" : "1")} AS Active
                    FROM FeederConsumers fc
                    JOIN Feeders f ON f.Id = fc.FeederId
                    ORDER BY fc.FeederId, fc.Position
                    """).ToList();
                List<OldInactiveFrom> inactiveFrom = tables.Contains("ConsumerInactiveFrom")
                    ? c.Query<OldInactiveFrom>("SELECT ConsumerId, BillMonth FROM ConsumerInactiveFrom").ToList()
                    : [];
                List<OldReading> readings = tables.Contains("Readings")
                    ? c.Query<OldReading>("SELECT ConsumerId, BillMonth, Kwh, Status FROM Readings").ToList()
                    : [];
                return new OldData(feeders, consumers, inactiveFrom, readings);
            }
            catch (SqliteException ex)
            {
                throw new LegacyImportException($"{Path.GetFileName(path)} could not be read as an old Feeder kWh Copy database. {ex.Message}");
            }
        }

        private static DateTime CreatedAt(OldFeeder feeder) =>
            DateTime.TryParseExact(feeder.CreatedAt, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime created)
                ? created
                : throw new LegacyImportException($"Feeder {feeder.Name} has an unreadable creation date ({feeder.CreatedAt}).");

        private sealed record OldData(List<OldFeeder> Feeders, List<OldConsumer> Consumers, List<OldInactiveFrom> InactiveFrom, List<OldReading> Readings);

        private sealed class OldFeeder
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string CreatedAt { get; set; } = string.Empty;
        }

        private sealed class OldConsumer
        {
            public long FeederId { get; set; }
            public string? BusId { get; set; }
            public string ConsumerId { get; set; } = string.Empty;
            public long Position { get; set; }
            public long Active { get; set; }
        }

        private sealed class OldInactiveFrom
        {
            public string ConsumerId { get; set; } = string.Empty;
            public string BillMonth { get; set; } = string.Empty;
        }

        private sealed class OldReading
        {
            public string ConsumerId { get; set; } = string.Empty;
            public string BillMonth { get; set; } = string.Empty;
            public double? Kwh { get; set; }
            public string Status { get; set; } = string.Empty;
        }
    }
}
