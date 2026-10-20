using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Core.Models.Import
{
    public sealed record RowError(int RowNumber, string Reason);

    public sealed record ParsedSheet(string Name, IReadOnlyList<ConsumerEntry> Entries, int SkippedRows);

    public sealed record ParsedWorkbook(string Path, IReadOnlyList<ParsedSheet> Feeders, IReadOnlyList<string> Notes, IReadOnlyList<string> Problems);
}
