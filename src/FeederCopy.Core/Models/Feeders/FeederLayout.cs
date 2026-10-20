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
                && FamilyInsertIndex(list, bus) is int index)
            {
                list.Insert(index, normalized);
                return Arrange(list);
            }
            return Arrange(list.Append(normalized));
        }

        private static int? FamilyInsertIndex(IReadOnlyList<ConsumerEntry> arranged, string bus)
        {
            if (SplitNumber(bus) is not (string prefix, long number))
                return null;

            var siblings = arranged
                .Select(e => e.BusId)
                .OfType<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(b => (Bus: b, Parts: SplitNumber(b)))
                .Where(s => s.Parts is (string p, _) && string.Equals(p, prefix, StringComparison.OrdinalIgnoreCase))
                .Select(s => (s.Bus, Number: s.Parts!.Value.Number))
                .ToList();
            if (siblings.Count == 0)
                return null;

            var lower = siblings.Where(s => s.Number < number).OrderByDescending(s => s.Number).ToList();
            if (lower.Count > 0)
                return LastIndexOfBus(arranged, lower[0].Bus) + 1;
            string next = siblings.OrderBy(s => s.Number).First().Bus;
            return arranged.ToList().FindIndex(e => string.Equals(e.BusId, next, StringComparison.OrdinalIgnoreCase));
        }

        private static int LastIndexOfBus(IReadOnlyList<ConsumerEntry> arranged, string bus) =>
            arranged.ToList().FindLastIndex(e => string.Equals(e.BusId, bus, StringComparison.OrdinalIgnoreCase));

        private static (string Prefix, long Number)? SplitNumber(string bus)
        {
            int start = bus.Length;
            while (start > 0 && char.IsAsciiDigit(bus[start - 1]))
                start--;
            if (start == bus.Length || start == 0)
                return null;
            return long.TryParse(bus[start..], out long number) ? (bus[..start], number) : null;
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
