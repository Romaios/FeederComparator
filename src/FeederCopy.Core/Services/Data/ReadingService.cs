using Dapper;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
using Microsoft.Data.Sqlite;

namespace FeederCopy.Core.Services.Data
{
    public sealed class ReadingService : IReadingService
    {
        private readonly Database _database;

        public ReadingService(Database database) => _database = database;

        public void SaveMonth(BillMonth month, IReadOnlyList<ReadingRecord> readings)
        {
            using SqliteConnection c = _database.Open();
            using SqliteTransaction tx = c.BeginTransaction();
            c.Execute("DELETE FROM Readings WHERE BillMonth = @month", new { month = month.Key }, tx);

            using SqliteCommand cmd = c.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT OR REPLACE INTO Readings (ConsumerId, BillMonth, Kwh, Status) VALUES ($consumer, $month, $kwh, $status)";
            SqliteParameter consumer = cmd.Parameters.Add("$consumer", SqliteType.Text);
            SqliteParameter billMonth = cmd.Parameters.Add("$month", SqliteType.Text);
            SqliteParameter kwh = cmd.Parameters.Add("$kwh", SqliteType.Real);
            SqliteParameter status = cmd.Parameters.Add("$status", SqliteType.Text);
            cmd.Prepare();

            billMonth.Value = month.Key;
            foreach (ReadingRecord reading in readings)
            {
                consumer.Value = reading.ConsumerId;
                kwh.Value = (object?)reading.Kwh ?? DBNull.Value;
                status.Value = reading.Status;
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }

        public IReadOnlyList<int> GetYears()
        {
            using SqliteConnection c = _database.Open();
            return c.Query<string>("SELECT DISTINCT substr(BillMonth, 1, 4) FROM Readings ORDER BY 1 DESC")
                .Select(int.Parse)
                .ToList();
        }

        public IReadOnlyList<ReadingRecord> GetYear(int year)
        {
            using SqliteConnection c = _database.Open();
            return c.Query<(string ConsumerId, string BillMonth, double? Kwh, string Status)>(
                    "SELECT ConsumerId, BillMonth, Kwh, Status FROM Readings WHERE BillMonth >= @from AND BillMonth <= @to",
                    new { from = $"{year:D4}01", to = $"{year:D4}12" })
                .Select(r => new ReadingRecord(r.ConsumerId, r.BillMonth, r.Kwh, r.Status))
                .ToList();
        }
    }
}
