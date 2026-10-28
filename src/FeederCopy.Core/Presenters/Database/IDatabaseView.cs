namespace FeederCopy.Core.Presenters.Database
{
    public enum DatabaseTone
    {
        Info,
        Ok,
        Warning,
        Error,
    }

    public sealed record DatabaseForm(string Host, string Port, string Database, string User, string Password);

    public interface IDatabaseView
    {
        void ShowConnection(string host, int port, string database, string user, bool hasPassword);
        void ShowStatus(string message, DatabaseTone tone);
        void SetBusy(bool busy);
        void OpenScriptsFolder();
        string? PickOldDatabase();
        void ShowImportResult(string message, DatabaseTone tone);
    }
}
