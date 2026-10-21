namespace FeederCopy.Core.Models.Feeders
{
    public static class FeederLayout
    {
        public static string? NormalizeBus(string? text)
        {
            string? trimmed = text?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        public static string NormalizeConsumer(string text) => text.Trim();

        public static List<ConsumerEntry> Arrange(IEnumerable<ConsumerEntry> entries)
        {
            var groups = new List<(string Spelling, List<ConsumerEntry> Items)>();
            var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var unknown = new List<ConsumerEntry>();

            foreach (ConsumerEntry entry in entries)
            {
                string? bus = NormalizeBus(entry.BusId);
                if (bus is null)
                {
                    unknown.Add(entry with { BusId = null });
                    continue;
                }
                if (!index.TryGetValue(bus, out int i))
                {
                    i = groups.Count;
                    index[bus] = i;
                    groups.Add((bus, []));
                }
                groups[i].Items.Add(entry with { BusId = groups[i].Spelling });
            }

            return groups.SelectMany(g => g.Items).Concat(unknown).ToList();
        }

        public static List<ConsumerEntry> Add(IReadOnlyList<ConsumerEntry> entries, string? busId, string consumerId) =>
            Add(entries, new ConsumerEntry(busId, consumerId));

        public static List<ConsumerEntry> Add(IReadOnlyList<ConsumerEntry> entries, ConsumerEntry entry)
        {
            ConsumerEntry normalized = entry with { BusId = NormalizeBus(entry.BusId), ConsumerId = NormalizeConsumer(entry.ConsumerId) };
            List<ConsumerEntry> list = Arrange(entries);
            if (normalized.BusId is string bus
                && !list.Any(e => string.Equals(e.BusId, bus, StringComparison.OrdinalIgnoreCase))
                && BusOrder.InsertIndex(Buses(list), bus) is int block)
            {
                list.Insert(EntryIndexOfBlock(list, block), normalized);
                return Arrange(list);
            }
            return Arrange(list.Append(normalized));
        }

        public static List<ConsumerEntry> AddAfter(IReadOnlyList<ConsumerEntry> entries, ConsumerEntry entry, string afterBus)
        {
            ConsumerEntry normalized = entry with { BusId = NormalizeBus(entry.BusId), ConsumerId = NormalizeConsumer(entry.ConsumerId) };
            List<ConsumerEntry> list = Arrange(entries);
            bool busExists = list.Any(e => string.Equals(e.BusId, normalized.BusId, StringComparison.OrdinalIgnoreCase));
            int last = list.FindLastIndex(e => string.Equals(e.BusId, afterBus.Trim(), StringComparison.OrdinalIgnoreCase));
            if (busExists || normalized.BusId is null || last < 0)
                return Add(list, normalized);
            list.Insert(last + 1, normalized);
            return Arrange(list);
        }

        private static int EntryIndexOfBlock(IReadOnlyList<ConsumerEntry> arranged, int block)
        {
            int seen = -1;
            string? previous = null;
            for (int i = 0; i < arranged.Count; i++)
            {
                string? bus = arranged[i].BusId;
                if (bus is null)
                    return i;
                if (!string.Equals(bus, previous, StringComparison.OrdinalIgnoreCase))
                {
                    seen++;
                    previous = bus;
                    if (seen == block)
                        return i;
                }
            }
            return arranged.Count;
        }

        public static List<ConsumerEntry> Remove(IReadOnlyList<ConsumerEntry> entries, string consumerId)
        {
            string id = NormalizeConsumer(consumerId);
            return entries.Where(e => !string.Equals(e.ConsumerId, id, StringComparison.Ordinal)).ToList();
        }

        public static List<ConsumerEntry> SetBus(IReadOnlyList<ConsumerEntry> entries, string consumerId, string? busId)
        {
            string id = NormalizeConsumer(consumerId);
            bool active = entries.FirstOrDefault(e => string.Equals(e.ConsumerId, id, StringComparison.Ordinal))?.Active ?? true;
            return Add(Remove(entries, id), new ConsumerEntry(busId, id, active));
        }

        public static IReadOnlyList<string> Buses(IReadOnlyList<ConsumerEntry> entries) =>
            entries.Where(e => e.BusId is not null)
                .Select(e => e.BusId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        public static IReadOnlyList<ConsumerRow> ToRows(IReadOnlyList<ConsumerEntry> entries) =>
            entries.Select((e, i) => new ConsumerRow(i + 1, BusLabels.For(e.BusId), e.ConsumerId, e.Active)).ToList();
    }
}
