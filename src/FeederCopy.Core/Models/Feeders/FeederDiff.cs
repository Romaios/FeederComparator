namespace FeederCopy.Core.Models.Feeders
{
    public sealed record FeederDiffResult(int Added, int Removed, int BusChanged)
    {
        public override string ToString() => $"{Added} added, {Removed} removed, {BusChanged} moved to a different bus";
    }

    public static class FeederDiff
    {
        public static FeederDiffResult Compare(IReadOnlyList<ConsumerEntry> saved, IReadOnlyList<ConsumerEntry> incoming)
        {
            Dictionary<string, string?> before = saved.ToDictionary(e => e.ConsumerId, e => e.BusId, StringComparer.Ordinal);
            var after = incoming.Select(e => e.ConsumerId).ToHashSet(StringComparer.Ordinal);

            int added = 0;
            int moved = 0;
            foreach (ConsumerEntry entry in incoming)
            {
                if (!before.TryGetValue(entry.ConsumerId, out string? oldBus))
                    added++;
                else if (!string.Equals(oldBus, entry.BusId, StringComparison.OrdinalIgnoreCase))
                    moved++;
            }
            int removed = saved.Count(e => !after.Contains(e.ConsumerId));
            return new FeederDiffResult(added, removed, moved);
        }
    }
}
