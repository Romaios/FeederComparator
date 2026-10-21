namespace FeederCopy.Core.Models.Feeders
{
    public sealed record BusBlock(string? Bus, IReadOnlyList<ConsumerEntry> Entries);

    public sealed record BusBlockRow(string Bus, int From, int To, int NewFrom, int NewTo, string? HandPlacedTag)
    {
        public int Count => NewTo - NewFrom + 1;

        public bool Moves => From != NewFrom || To != NewTo;
    }

    public static class BusBlocks
    {
        public static List<BusBlock> Split(IEnumerable<ConsumerEntry> entries)
        {
            var blocks = new List<BusBlock>();
            List<ConsumerEntry>? run = null;
            string? bus = null;
            foreach (ConsumerEntry entry in entries)
            {
                string? next = FeederLayout.NormalizeBus(entry.BusId);
                if (run is null || !string.Equals(next, bus, StringComparison.OrdinalIgnoreCase))
                {
                    run = [];
                    bus = next;
                    blocks.Add(new BusBlock(bus, run));
                }
                run.Add(entry);
            }
            return blocks;
        }

        public static List<ConsumerEntry> Flatten(IEnumerable<BusBlock> blocks) => blocks.SelectMany(b => b.Entries).ToList();

        public static List<BusBlock> Sort(IReadOnlyList<BusBlock> blocks, IReadOnlyList<BusTagRule> rules) =>
            Split(BusSorter.Sort(Flatten(blocks), rules));

        public static (List<BusBlock> Blocks, IReadOnlyList<int> Moved) Move(IReadOnlyList<BusBlock> blocks, IReadOnlyCollection<int> indexes, int insertAt)
        {
            var selected = indexes.Where(i => i >= 0 && i < blocks.Count).Distinct().Order().ToList();
            if (selected.Count == 0)
                return (blocks.ToList(), []);
            int target = Math.Clamp(insertAt, 0, blocks.Count) - selected.Count(i => i < insertAt);
            var picked = new HashSet<int>(selected);
            List<BusBlock> rest = blocks.Where((_, i) => !picked.Contains(i)).ToList();
            rest.InsertRange(target, selected.Select(i => blocks[i]));
            return (rest, Enumerable.Range(target, selected.Count).ToList());
        }

        public static IReadOnlyList<BusBlockRow> Rows(IReadOnlyList<ConsumerEntry> saved, IReadOnlyList<BusBlock> draft, Func<string, string?> tagOf)
        {
            var savedRow = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < saved.Count; i++)
                savedRow.TryAdd(saved[i].ConsumerId, i + 1);

            var rows = new List<BusBlockRow>(draft.Count);
            int next = 1;
            foreach (BusBlock block in draft)
            {
                int from = int.MaxValue;
                int to = int.MinValue;
                for (int i = 0; i < block.Entries.Count; i++)
                {
                    int row = savedRow.GetValueOrDefault(block.Entries[i].ConsumerId, next + i);
                    from = Math.Min(from, row);
                    to = Math.Max(to, row);
                }
                string? tag = block.Bus is null ? null : tagOf(block.Bus);
                rows.Add(new BusBlockRow(BusLabels.For(block.Bus), from, to, next, next + block.Entries.Count - 1, tag));
                next += block.Entries.Count;
            }
            return rows;
        }
    }
}
