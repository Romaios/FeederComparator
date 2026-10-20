using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Import;
using FeederCopy.Core.Models.Settings;

namespace FeederCopy.Core.Services.Import
{
    public static class FeederListParser
    {
        public static ParsedWorkbook ReadWorkbook(string path, FeederColumns columns)
        {
            try
            {
                IReadOnlyList<TableSheet> sheets = TableReaderFactory.For(path).ReadSheets(path);
                var feeders = new List<ParsedSheet>();
                var notes = new List<string>();
                var problems = new List<string>();
                string? missingColumns = null;
                bool anyContent = false;

                foreach (TableSheet sheet in sheets)
                {
                    int headerIndex = FindHeader(sheet.Rows);
                    if (headerIndex < 0)
                        continue;
                    anyContent = true;

                    IReadOnlyList<int> map;
                    try
                    {
                        map = HeaderIndex.Require(sheet.Rows[headerIndex], [columns.BusId, columns.ConsumerId]);
                    }
                    catch (InvalidDataException ex)
                    {
                        missingColumns ??= ex.Message;
                        notes.Add($"Sheet {sheet.Name} skipped: it has no {columns.BusId} and {columns.ConsumerId} columns.");
                        continue;
                    }

                    if (ReadSheet(sheet, headerIndex, map, notes, problems) is { } parsed)
                        feeders.Add(parsed);
                }

                if (!anyContent)
                    problems.Add("The file is empty.");
                else if (feeders.Count == 0 && problems.Count == 0)
                    problems.Add(missingColumns ?? "No consumers were found in the file.");
                problems.AddRange(CrossSheetDuplicates(feeders));

                return new ParsedWorkbook(path, feeders, notes, problems);
            }
            catch (Exception ex) when (ReadFailure.Describe(ex) is string message)
            {
                return new ParsedWorkbook(path, [], [], [message]);
            }
        }

        private static ParsedSheet? ReadSheet(TableSheet sheet, int headerIndex, IReadOnlyList<int> map, List<string> notes, List<string> problems)
        {
            var entries = new List<ConsumerEntry>();
            var rowsById = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            int skipped = 0;

            for (int i = headerIndex + 1; i < sheet.Rows.Count; i++)
            {
                IReadOnlyList<string?> cells = sheet.Rows[i];
                int rowNumber = i + 1;
                if (HeaderIndex.IsBlank(cells))
                    continue;

                string? bus = HeaderIndex.Cell(cells, map[0]);
                string? consumer = HeaderIndex.Cell(cells, map[1]);
                if (consumer is null)
                {
                    if (bus is not null)
                    {
                        notes.Add($"{sheet.Name} row {rowNumber}: Bus ID without a consumer ID; skipped");
                        skipped++;
                    }
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

            foreach ((string id, List<int> rows) in rowsById.Where(p => p.Value.Count > 1))
                problems.Add($"Sheet {sheet.Name}: consumer ID {id} appears more than once (rows {string.Join(", ", rows)}).");

            if (entries.Count == 0)
            {
                notes.Add($"Sheet {sheet.Name} skipped: no consumers.");
                return null;
            }
            return new ParsedSheet(sheet.Name.Trim(), FeederLayout.Arrange(entries), skipped);
        }

        private static IEnumerable<string> CrossSheetDuplicates(IReadOnlyList<ParsedSheet> feeders)
        {
            var firstSheet = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (ParsedSheet feeder in feeders)
            {
                foreach (ConsumerEntry entry in feeder.Entries)
                {
                    if (firstSheet.TryGetValue(entry.ConsumerId, out string? other))
                        yield return $"Consumer ID {entry.ConsumerId} is on both the {other} and {feeder.Name} sheets.";
                    else
                        firstSheet[entry.ConsumerId] = feeder.Name;
                }
            }
        }

        private static int FindHeader(IReadOnlyList<IReadOnlyList<string?>> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (!HeaderIndex.IsBlank(rows[i]))
                    return i;
            }
            return -1;
        }
    }
}
