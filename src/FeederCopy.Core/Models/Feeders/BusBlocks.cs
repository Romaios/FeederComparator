namespace FeederCopy.Core.Models.Feeders
{
    public sealed record BusBlockRow(string Bus, int From, int To, int NewFrom, int NewTo, string? HandPlacedTag)
    {
        public int Count => To - From + 1;

        public bool Moves => From != NewFrom || To != NewTo;
    }

    public static class BusBlocks
    {
        public static IReadOnlyList<BusBlockRow> Plan(IReadOnlyList<ConsumerEntry> current, IReadOnlyList<ConsumerEntry> sorted, IReadOnlyList<BusTagRule> rules)
        {
            var sortedIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < sorted.Count; i++)
                sortedIndex.TryAdd(sorted[i].ConsumerId, i);

            var rows = new List<BusBlockRow>();
            int start = 0;
            while (start < current.Count)
            {
                string? bus = FeederLayout.NormalizeBus(current[start].BusId);
                int end = start;
                while (end + 1 < current.Count && string.Equals(FeederLayout.NormalizeBus(current[end + 1].BusId), bus, StringComparison.OrdinalIgnoreCase))
                    end++;

                int newFrom = int.MaxValue;
                int newTo = int.MinValue;
                for (int i = start; i <= end; i++)
                {
                    int at = sortedIndex.GetValueOrDefault(current[i].ConsumerId, i);
                    newFrom = Math.Min(newFrom, at);
                    newTo = Math.Max(newTo, at);
                }
                string? tag = bus is null ? null : BusTags.MatchedTag(bus, rules);
                rows.Add(new BusBlockRow(BusLabels.For(bus), start + 1, end + 1, newFrom + 1, newTo + 1, tag));
                start = end + 1;
            }
            return rows;
        }
    }
}
