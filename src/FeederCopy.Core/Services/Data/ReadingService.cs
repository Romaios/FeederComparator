using Dapper;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
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
            return c.Query<string>("SELECT DISTINCT SUBSTRING(BillMonth, 1, 4) FROM Readings ORDER BY 1 DESC")
                .Select(int.Parse)
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
    }
}
