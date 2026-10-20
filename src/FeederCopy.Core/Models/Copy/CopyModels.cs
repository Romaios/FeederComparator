using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Core.Models.Copy
{
    public sealed record FeederSource(string Name, IReadOnlyList<ConsumerEntry> Consumers);

    public sealed record FilledRow(int Count, string BusLabel, string ConsumerId, double? Kwh, string? Problem);

    public sealed record FilledFeeder(string Name, IReadOnlyList<FilledRow> Rows);

    public sealed record FeederTotals(string Name, int Consumers, int Copied, int NotFound, int Duplicates, int Unreadable, double TotalKwh);

    public sealed record CopyProblem(string Feeder, int Count, string BusLabel, string ConsumerId, string Problem);

    public sealed record CopyResult(
        BillMonth Month,
        string SourceFile,
        IReadOnlyList<FilledFeeder> Feeders,
        IReadOnlyList<FeederTotals> Totals,
        IReadOnlyList<CopyProblem> Problems);
}
