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

        public static List<ConsumerEntry> Add(IReadOnlyList<ConsumerEntry> entries, ConsumerEntry entry) =>
            Arrange(entries.Append(entry with { BusId = NormalizeBus(entry.BusId), ConsumerId = NormalizeConsumer(entry.ConsumerId) }));

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
