namespace FeederCopy.Core.Services.Import
{
    public interface ITableReader
    {
        IEnumerable<IReadOnlyList<string?>> ReadRows(string path);
    }
}
