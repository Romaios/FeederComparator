using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Import;
using FeederCopy.Core.Models.Settings;

namespace FeederCopy.Core.Services.Import
{
    public static class FeederListParser
    {
        private sealed record Occurrence(int SheetIndex, string Sheet, int Row, string? BusId, string ConsumerId, bool Active)
        {
            public string Where => $"{Sheet} row {Row}";
        }

        private sealed record SheetRead(string Name, List<Occurrence> Rows, int SkippedRows);

        public static ParsedWorkbook ReadWorkbook(string path, FeederColumns columns)
        {
            try
            {
                IReadOnlyList<TableSheet> sheets = TableReaderFactory.For(path).ReadSheets(path);
                var reads = new List<SheetRead>();
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

                    int? statusIndex = string.IsNullOrWhiteSpace(columns.Status) ? null : HeaderIndex.Find(sheet.Rows[headerIndex], columns.Status);
                    SheetRead read = ReadSheet(sheet, reads.Count, headerIndex, map, statusIndex, notes, problems);
                    if (read.Rows.Count == 0)
                        notes.Add($"Sheet {sheet.Name} skipped: no consumers.");
                    else
                        reads.Add(read);
                }

                if (!anyContent)
                    problems.Add("The file is empty.");
                else if (reads.Count == 0 && problems.Count == 0)
                    problems.Add(missingColumns ?? "No consumers were found in the file.");

                HashSet<Occurrence> kept = ResolveDuplicates(reads.SelectMany(r => r.Rows), notes);
                List<ParsedSheet> feeders = reads
                    .Select(r => new ParsedSheet(
                        r.Name.Trim(),
                        FeederLayout.Arrange(r.Rows.Where(kept.Contains).Select(o => new ConsumerEntry(o.BusId, o.ConsumerId, o.Active))),
                        r.SkippedRows))
                    .Where(f => f.Entries.Count > 0)
                    .ToList();

                return new ParsedWorkbook(path, feeders, notes, problems);
            }
            catch (Exception ex) when (ReadFailure.Describe(ex) is string message)
            {
                return new ParsedWorkbook(path, [], [], [message]);
            }
        }

        private static SheetRead ReadSheet(TableSheet sheet, int sheetIndex, int headerIndex, IReadOnlyList<int> map, int? statusIndex, List<string> notes, List<string> problems)
        {
            var rows = new List<Occurrence>();
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

                string? status = statusIndex is int s ? HeaderIndex.Cell(cells, s) : null;
                bool? active = status switch
                {
                    null => true,
                    "1" => true,
                    "0" => false,
                    _ => null,
                };
                if (active is null)
                {
                    problems.Add($"Sheet {sheet.Name} row {rowNumber}: status '{status}' must be 1 or 0.");
                    continue;
                }

                rows.Add(new Occurrence(sheetIndex, sheet.Name, rowNumber, bus, consumer, active.Value));
            }

            return new SheetRead(sheet.Name, rows, skipped);
        }

        private static HashSet<Occurrence> ResolveDuplicates(IEnumerable<Occurrence> all, List<string> notes)
        {
            var kept = new HashSet<Occurrence>();
            foreach (IGrouping<string, Occurrence> group in all.GroupBy(o => o.ConsumerId, StringComparer.Ordinal))
            {
                List<Occurrence> copies = group.ToList();
                if (copies.Count == 1)
                {
                    kept.Add(copies[0]);
                    continue;
                }

                List<Occurrence> active = copies.Where(o => o.Active).ToList();
                if (active.Count > 1)
                {
                    kept.Add(active[0]);
                    notes.Add($"Consumer ID {group.Key} appears more than once ({string.Join(", ", copies.Select(o => o.Where))}); kept {active[0].Where}.");
                }
                else if (active.Count == 1)
                {
                    kept.Add(active[0]);
                    string dropped = string.Join(", ", copies.Where(o => !o.Active).Select(o => o.Where));
                    notes.Add($"Consumer ID {group.Key}: kept the active one ({active[0].Where}); dropped the inactive copy ({dropped}).");
                }
                else
                {
                    kept.Add(copies[0]);
                    notes.Add($"Consumer ID {group.Key} is inactive in {string.Join(", ", copies.Select(o => o.Where))}; kept {copies[0].Where}.");
                }
            }
            return kept;
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
