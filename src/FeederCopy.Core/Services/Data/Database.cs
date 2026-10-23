using Dapper;
using MySqlConnector;

namespace FeederCopy.Core.Services.Data
{
    public sealed record DatabaseConnection(string Host, int Port, string Database, string User, string Password)
    {
        public const int DefaultPort = 3306;
        public const string DefaultHost = "localhost";

        public static DatabaseConnection Empty { get; } = new(DefaultHost, DefaultPort, string.Empty, string.Empty, string.Empty);

        public bool IsComplete =>
            !string.IsNullOrWhiteSpace(Host) && Port is > 0 and <= 65535
            && !string.IsNullOrWhiteSpace(Database) && !string.IsNullOrWhiteSpace(User);
    }

    public sealed record DatabaseCheck(string Version, IReadOnlyList<string> MissingScripts)
    {
        public bool Ready => MissingScripts.Count == 0;
    }

    public sealed class Database
    {
        private const string SchemaPrefix = "FeederCopy.Schema.";
        private const string SchemaSuffix = ".sql";
        private const int ConnectSeconds = 5;
        private const int CommandSeconds = 300;

        public Database(DatabaseConnection connection) => Connection = connection;

        public DatabaseConnection Connection { get; private set; }

        public static IReadOnlyList<string> ScriptNames { get; } = typeof(Database).Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(SchemaPrefix, StringComparison.Ordinal) && name.EndsWith(SchemaSuffix, StringComparison.Ordinal))
            .Select(name => name[SchemaPrefix.Length..^SchemaSuffix.Length])
            .Order(StringComparer.Ordinal)
            .ToList();

        public void Use(DatabaseConnection connection) => Connection = connection;

        public MySqlConnection Open()
        {
            if (!Connection.IsComplete)
                throw new InvalidOperationException("The database connection is not set. Enter it in Settings → Database.");
            var connection = new MySqlConnection(ConnectionString(Connection));
            connection.Open();
            return connection;
        }

        public DatabaseCheck Check()
        {
            using MySqlConnection c = Open();
            string version = c.ExecuteScalar<string>("SELECT VERSION()") ?? string.Empty;
            bool tracked = c.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'SchemaScripts'") > 0;
            HashSet<string> applied = tracked
                ? c.Query<string>("SELECT Name FROM SchemaScripts").ToHashSet(StringComparer.OrdinalIgnoreCase)
                : [];
            return new DatabaseCheck(version, ScriptNames.Where(name => !applied.Contains(name)).ToList());
        }

        public static string ScriptText(string name)
        {
            using Stream stream = typeof(Database).Assembly.GetManifestResourceStream(SchemaPrefix + name + SchemaSuffix)
                ?? throw new InvalidOperationException($"The database script {name} is missing from the program.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static string ConnectionString(DatabaseConnection connection) => new MySqlConnectionStringBuilder
        {
            Server = connection.Host.Trim(),
            Port = (uint)connection.Port,
            Database = connection.Database.Trim(),
            UserID = connection.User.Trim(),
            Password = connection.Password,
            ConnectionTimeout = ConnectSeconds,
            DefaultCommandTimeout = CommandSeconds,
            CharacterSet = "utf8mb4",
        }.ConnectionString;
    }
}
