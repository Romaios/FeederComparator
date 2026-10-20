using Dapper;
using Microsoft.Data.Sqlite;

namespace FeederCopy.Core.Services.Data
{
    public sealed class Database
    {
        private const string SchemaPrefix = "FeederCopy.Schema.";
        private readonly string _connectionString;

        public string FilePath { get; }

        public Database(string filePath)
        {
            FilePath = filePath;
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = filePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
            }.ToString();
        }

        public SqliteConnection Open()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            connection.Execute("PRAGMA synchronous = NORMAL; PRAGMA foreign_keys = ON;");
            return connection;
        }

        public void EnsureSchema()
        {
            bool isNew = !File.Exists(FilePath);
            IReadOnlyList<string> scripts = LoadSchemaScripts();
            try
            {
                using SqliteConnection c = Open();
                long version = c.ExecuteScalar<long>("PRAGMA user_version");
                if (version > scripts.Count)
                    throw new InvalidOperationException($"feeders.db was created by a newer version of the program (schema {version}; this program knows {scripts.Count}). Install the newer program.");
                if (version == scripts.Count)
                    return;

                using SqliteTransaction tx = c.BeginTransaction();
                foreach (string script in scripts.Skip((int)version))
                    c.Execute(script, transaction: tx);
                c.Execute($"PRAGMA user_version = {scripts.Count}", transaction: tx);
                tx.Commit();
            }
            catch (Exception) when (isNew)
            {
                SqliteConnection.ClearAllPools();
                File.Delete(FilePath);
                throw;
            }
        }

        private static IReadOnlyList<string> LoadSchemaScripts()
        {
            var assembly = typeof(Database).Assembly;
            List<string> names = assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(SchemaPrefix, StringComparison.Ordinal))
                .Order(StringComparer.Ordinal)
                .ToList();
            if (names.Count == 0)
                throw new InvalidOperationException("The database schema scripts are missing from the program.");

            return names.Select(name =>
            {
                using Stream stream = assembly.GetManifestResourceStream(name)!;
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }).ToList();
        }
    }
}
