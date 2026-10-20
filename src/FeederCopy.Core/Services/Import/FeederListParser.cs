using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Import;
using FeederCopy.Core.Models.Settings;

namespace FeederCopy.Core.Services.Import
{
    public static class FeederListParser
    {
        public static ParsedFeederList Read(string path, FeederColumns columns)
        {
            try
            {
                ITableReader reader = TableReaderFactory.For(path);
                IReadOnlyList<int>? map = null;
                var entries = new List<ConsumerEntry>();
                var rowsById = new Dictionary<string, List<int>>(StringComparer.Ordinal);
                var warnings = new List<RowError>();
                int rowNumber = 0;

                foreach (IReadOnlyList<string?> cells in reader.ReadRows(path))
                {
                    rowNumber++;
                    if (HeaderIndex.IsBlank(cells))
                        continue;
                    if (map is null)
                    {
                        map = HeaderIndex.Require(cells, [columns.BusId, columns.ConsumerId]);
                        continue;
                    }

                    string? bus = HeaderIndex.Cell(cells, map[0]);
                    string? consumer = HeaderIndex.Cell(cells, map[1]);
                    if (consumer is null)
                    {
                        if (bus is not null)
                            warnings.Add(new RowError(rowNumber, "Bus ID without a consumer ID; skipped"));
                        continue;
                    }

                    if (!rowsById.TryGetValue(consumer, out List<int>? rows))
                    {
                        rows = [];
                        rowsById[consumer] = rows;
                        entries.Add(new ConsumerEntry(bus, consumer));
                    }
                    rows.Add(rowNumber);
                }

                if (map is null)
                    return Fail(path, "The file is empty.");
                KeyValuePair<string, List<int>> duplicate = rowsById.FirstOrDefault(p => p.Value.Count > 1);
                if (duplicate.Value is not null)
                    return Fail(path, $"Consumer ID {duplicate.Key} appears more than once (rows {string.Join(", ", duplicate.Value)}).");
                if (entries.Count == 0)
                    return Fail(path, "No consumers were found in the file.");

                return new ParsedFeederList(path, FeederLayout.Arrange(entries), warnings, null);
            }
            catch (Exception ex) when (ReadFailure.Describe(ex) is string message)
            {
                return Fail(path, message);
            }
        }

        private static ParsedFeederList Fail(string path, string message) => new(path, [], [], message);
    }
}
