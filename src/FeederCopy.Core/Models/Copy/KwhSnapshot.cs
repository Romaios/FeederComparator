using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Import;

namespace FeederCopy.Core.Models.Copy
{
    public sealed record AsBilledEntry(int RowNumber, double? Kwh, bool Blank = false);

    public sealed record KwhSnapshot(
        string FileName,
        BillMonth? Month,
        IReadOnlyDictionary<string, IReadOnlyList<AsBilledEntry>> ByAccount,
        int RowsRead,
        IReadOnlyList<RowError> Errors,
        string? FatalError);
}
