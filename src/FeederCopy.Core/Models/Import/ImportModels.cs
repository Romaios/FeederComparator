using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Core.Models.Import
{
    public sealed record RowError(int RowNumber, string Reason);

    public sealed record ParsedFeederList(string Path, IReadOnlyList<ConsumerEntry> Entries, IReadOnlyList<RowError> Warnings, string? FatalError);
}
