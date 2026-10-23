using System.Text;
using Dapper;
using MySqlConnector;

namespace FeederCopy.Core.Services.Data
{
    internal static class MySqlBulk
    {
        private const int RowsPerStatement = 500;

        public static void Insert(MySqlConnection c, MySqlTransaction tx, string insertInto, IReadOnlyList<object?[]> rows, string? suffix = null)
        {
            foreach (object?[][] chunk in rows.Chunk(RowsPerStatement))
            {
                var sql = new StringBuilder(insertInto).Append(" VALUES ");
                var parameters = new DynamicParameters();
                for (int r = 0; r < chunk.Length; r++)
                {
                    sql.Append(r == 0 ? "(" : ",(");
                    for (int v = 0; v < chunk[r].Length; v++)
                    {
                        string name = $"p{r}_{v}";
                        sql.Append(v == 0 ? "@" : ",@").Append(name);
                        parameters.Add(name, chunk[r][v]);
                    }
                    sql.Append(')');
                }
                if (suffix is not null)
                    sql.Append(' ').Append(suffix);
                c.Execute(sql.ToString(), parameters, tx);
            }
        }

        public static void FillIdTable(MySqlConnection c, MySqlTransaction tx, string table, IEnumerable<(string ConsumerId, bool Active)> rows)
        {
            c.Execute($"DROP TEMPORARY TABLE IF EXISTS {table}", transaction: tx);
            c.Execute($"CREATE TEMPORARY TABLE {table} (ConsumerId VARCHAR(50) NOT NULL PRIMARY KEY, Active BOOLEAN NOT NULL)", transaction: tx);
            Insert(c, tx, $"INSERT IGNORE INTO {table} (ConsumerId, Active)", rows.Select(r => new object?[] { r.ConsumerId, r.Active }).ToList());
        }

        public static void DropTable(MySqlConnection c, MySqlTransaction tx, string table) =>
            c.Execute($"DROP TEMPORARY TABLE IF EXISTS {table}", transaction: tx);
    }
}
