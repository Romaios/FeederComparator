using Dapper;
using FeederCopy.Core.Services.Data;
using MySqlConnector;

namespace FeederCopy.Tests.Support
{
    internal sealed class TempDatabase : IDisposable
    {
        public TempDatabase(bool runScripts = true)
        {
            Name = "fctest_" + Guid.NewGuid().ToString("N")[..16];
            using (MySqlConnection server = OpenServer())
                server.Execute($"CREATE DATABASE {Name}");
            Db = new Database(Server with { Database = Name });
            if (!runScripts)
                return;
            foreach (string script in Database.ScriptNames)
                RunScript(script);
        }

        public void RunScript(string name)
        {
            var builder = new MySqlConnectionStringBuilder(Database.ConnectionString(Db.Connection)) { AllowUserVariables = true };
            using var c = new MySqlConnection(builder.ConnectionString);
            c.Open();
            c.Execute(Database.ScriptText(name));
        }

        public static DatabaseConnection Server { get; } = new(
            Environment.GetEnvironmentVariable("FEEDERCOPY_TEST_MYSQL_HOST") ?? "127.0.0.1",
            int.TryParse(Environment.GetEnvironmentVariable("FEEDERCOPY_TEST_MYSQL_PORT"), out int port) ? port : 3310,
            "mysql",
            Environment.GetEnvironmentVariable("FEEDERCOPY_TEST_MYSQL_USER") ?? "root",
            Environment.GetEnvironmentVariable("FEEDERCOPY_TEST_MYSQL_PASSWORD") ?? string.Empty);

        public string Name { get; }

        public Database Db { get; }

        public void Dispose()
        {
            MySqlConnection.ClearAllPools();
            using MySqlConnection server = OpenServer();
            server.Execute($"DROP DATABASE IF EXISTS {Name}");
        }

        private static MySqlConnection OpenServer()
        {
            var connection = new MySqlConnection(Database.ConnectionString(Server));
            connection.Open();
            return connection;
        }
    }
}
