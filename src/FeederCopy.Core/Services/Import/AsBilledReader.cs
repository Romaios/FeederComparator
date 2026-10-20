using System.Globalization;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Import;
using FeederCopy.Core.Models.Settings;

namespace FeederCopy.Core.Services.Import
{
    public sealed class AsBilledReader : IAsBilledReader
    {
        public KwhSnapshot Read(string path, AsBilledColumns columns, IProgress<int>? progress)
        {
            string fileName = Path.GetFileName(path);
            try
            {
                ITableReader reader = TableReaderFactory.For(path);
                IReadOnlyList<int>? map = null;
                var byAccount = new Dictionary<string, List<AsBilledEntry>>(StringComparer.Ordinal);
                var errors = new List<RowError>();
                var months = new SortedSet<BillMonth>();
                int rowNumber = 0;
                int rowsRead = 0;

                foreach (IReadOnlyList<string?> cells in reader.ReadRows(path))
                {
                    rowNumber++;
                    if (rowNumber % 5000 == 0)
                        progress?.Report(rowNumber);
                    if (HeaderIndex.IsBlank(cells))
                        continue;
                    if (map is null)
                    {
                        map = HeaderIndex.Require(cells, [columns.AccountNumber, columns.KwhUsed, columns.BillMonth]);
                        continue;
                    }

                    string? account = HeaderIndex.Cell(cells, map[0]);
                    if (account is null)
                    {
                        errors.Add(new RowError(rowNumber, "Missing account number"));
                        continue;
                    }
                    string? monthText = HeaderIndex.Cell(cells, map[2]);
                    if (!BillMonth.TryParseBillingText(monthText, out BillMonth month))
                    {
                        errors.Add(new RowError(rowNumber, $"Invalid bill month '{monthText}' (expected YYYYMM, MMYYYY or MMYY)"));
                        continue;
                    }

                    months.Add(month);
                    double? kwh = TryParseKwh(HeaderIndex.Cell(cells, map[1]), out double value) ? value : null;
                    if (!byAccount.TryGetValue(account, out List<AsBilledEntry>? entries))
                    {
                        entries = [];
                        byAccount[account] = entries;
                    }
                    entries.Add(new AsBilledEntry(rowNumber, kwh));
                    rowsRead++;
                }

                if (map is null)
                    return Fail(fileName, "The file is empty.");
                if (rowsRead == 0)
                    return Fail(fileName, "No valid rows were found in the file.");
                if (months.Count > 1)
                    return Fail(fileName, $"The file contains more than one bill month ({string.Join(", ", months)}). Use one month per file.");

                return new KwhSnapshot(
                    fileName,
                    months.Min,
                    byAccount.ToDictionary(p => p.Key, p => (IReadOnlyList<AsBilledEntry>)p.Value, StringComparer.Ordinal),
                    rowsRead,
                    errors,
                    null);
            }
            catch (Exception ex) when (ReadFailure.Describe(ex) is string message)
            {
                return Fail(fileName, message);
            }
        }

        public static bool TryParseKwh(string? text, out double kwh)
        {
            kwh = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;
            return double.TryParse(text.Replace(",", string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out kwh);
        }

        private static KwhSnapshot Fail(string fileName, string message) =>
            new(fileName, null, new Dictionary<string, IReadOnlyList<AsBilledEntry>>(), 0, [], message);
    }
}
