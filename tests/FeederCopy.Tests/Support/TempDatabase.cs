using FeederCopy.Core.Services.Data;
using Microsoft.Data.Sqlite;

namespace FeederCopy.Tests.Support
{
    internal sealed class TempDatabase : IDisposable
    {
        public string FilePath { get; } = TestFiles.TempPath(".db");
        public Database Db { get; }

        public TempDatabase()
        {
            Db = new Database(FilePath);
            Db.EnsureSchema();
        }

        public void Dispose()
        {
            ClearPool(FilePath);
            File.Delete(FilePath);
        }

        public static void ClearPool(string path)
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate }.ToString());
            SqliteConnection.ClearPool(connection);
        }
    }
}
