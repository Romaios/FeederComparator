using Dapper;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Monitoring;
using MySqlConnector;

namespace FeederCopy.Core.Services.Data
{
    public sealed class ReadingService : IReadingService
    {
        private readonly Database _database;

        public ReadingService(Database database) => _database = database;

        public void SaveMonth(BillMonth month, IReadOnlyList<ReadingRecord> readings)
        {
            using MySqlConnection c = _database.Open();
            using MySqlTransaction tx = c.BeginTransaction();
            c.Execute("DELETE FROM Readings WHERE BillMonth = @month", new { month = month.Key }, tx);
            List<object?[]> rows = readings
                .GroupBy(r => r.ConsumerId, StringComparer.Ordinal)
                .Select(g => g.Last())
                .Select(r => new object?[] { r.ConsumerId, month.Key, r.Kwh, r.Status })
                .ToList();
            MySqlBulk.Insert(c, tx, "INSERT INTO Readings (ConsumerId, BillMonth, Kwh, Status)", rows);
            tx.Commit();
        }

        public IReadOnlyList<int> GetYears()
        {
            using MySqlConnection c = _database.Open();
            return c.Query<string>("SELECT DISTINCT BillMonth FROM Readings")
                .Select(month => BillMonth.Parse(month).Year)
                .Distinct()
                .OrderDescending()
                .ToList();
        }

        public IReadOnlyList<ReadingRecord> GetYear(int year)
        {
            using MySqlConnection c = _database.Open();
            return c.Query<(string ConsumerId, string BillMonth, double? Kwh, string Status)>(
                    "SELECT ConsumerId, BillMonth, Kwh, Status FROM Readings WHERE BillMonth >= @from AND BillMonth <= @to",
                    new { from = $"{year:D4}01", to = $"{year:D4}12" })
                .Select(r => new ReadingRecord(r.ConsumerId, r.BillMonth, r.Kwh, r.Status))
                .ToList();
        }

        public IReadOnlyList<ReadingRecord> GetMonth(BillMonth month)
        {
            using MySqlConnection c = _database.Open();
            return c.Query<(string ConsumerId, string BillMonth, double? Kwh, string Status)>(
                    "SELECT ConsumerId, BillMonth, Kwh, Status FROM Readings WHERE BillMonth = @month",
                    new { month = month.Key })
                .Select(r => new ReadingRecord(r.ConsumerId, r.BillMonth, r.Kwh, r.Status))
                .ToList();
        }

        public IReadOnlyList<BillMonth> GetMonths()
        {
            using MySqlConnection c = _database.Open();
            return c.Query<string>("SELECT DISTINCT BillMonth FROM Readings ORDER BY BillMonth")
                .Select(BillMonth.Parse)
                .ToList();
        }

        public IReadOnlyList<FeederMonthTotals> GetFeederTotals(int year)
        {
            using MySqlConnection c = _database.Open();
            return c.Query<TotalsRow>(
                    """
                    SELECT f.Name AS Feeder, t.BillMonth, t.Consumers, t.Inactive, t.Duplicates, t.Unreadable, t.TotalKwh
                    FROM (
                        SELECT fc.FeederId, r.BillMonth,
                               COUNT(*) AS Consumers,
                               CAST(SUM(r.Status COLLATE utf8mb4_bin IN (@inactive, @missing)) AS SIGNED) AS Inactive,
                               CAST(SUM(r.Status COLLATE utf8mb4_bin = @duplicate) AS SIGNED) AS Duplicates,
                               CAST(SUM(r.Status COLLATE utf8mb4_bin = @unreadable) AS SIGNED) AS Unreadable,
                               COALESCE(SUM(r.Kwh), 0) AS TotalKwh
                        FROM Readings r
                        JOIN FeederConsumers fc ON fc.ConsumerId = r.ConsumerId
                        WHERE r.BillMonth >= @from AND r.BillMonth <= @to
                        GROUP BY fc.FeederId, r.BillMonth
                    ) t
                    JOIN Feeders f ON f.Id = t.FeederId
                    ORDER BY f.Name, t.BillMonth
                    """,
                    new
                    {
                        from = $"{year:D4}01",
                        to = $"{year:D4}12",
                        inactive = StatusLabels.Inactive,
                        missing = StatusLabels.MissingReading,
                        duplicate = StatusLabels.DuplicateInAsBilled,
                        unreadable = StatusLabels.Unreadable,
                    })
                .Select(r => new FeederMonthTotals(r.Feeder, BillMonth.Parse(r.BillMonth), (int)r.Consumers, (int)(r.Consumers - r.Inactive), (int)r.Inactive,
                    (int)r.Duplicates, (int)r.Unreadable, r.TotalKwh))
                .ToList();
        }

        private sealed class TotalsRow
        {
            public string Feeder { get; set; } = string.Empty;
            public string BillMonth { get; set; } = string.Empty;
            public long Consumers { get; set; }
            public long Inactive { get; set; }
            public long Duplicates { get; set; }
            public long Unreadable { get; set; }
            public double TotalKwh { get; set; }
        }
    }
}
