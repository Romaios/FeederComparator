namespace FeederCopy.Core.Services.Import
{
    public sealed record TableSheet(string Name, IReadOnlyList<IReadOnlyList<string?>> Rows);

    public interface ITableReader
    {
        IEnumerable<IReadOnlyList<string?>> ReadRows(string path);
        IReadOnlyList<TableSheet> ReadSheets(string path);
    }
}
