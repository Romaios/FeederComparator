namespace FeederCopy.Core.Models.Feeders
{
    public static class BusSorter
    {
        private sealed record Group(IReadOnlyList<string> Key, bool HandPlaced, List<ConsumerEntry> Rows);

        public static List<ConsumerEntry> Sort(IReadOnlyList<ConsumerEntry> entries, IReadOnlyList<BusTagRule> handPlacedTags)
        {
            List<ConsumerEntry> arranged = FeederLayout.Arrange(entries);
            var groups = new List<Group>();
            var unknown = new List<ConsumerEntry>();
            string? currentBus = null;
            List<ConsumerEntry>? block = null;

            foreach (ConsumerEntry entry in arranged)
            {
                if (entry.BusId is null)
                {
                    unknown.Add(entry);
                    continue;
                }
                if (!string.Equals(entry.BusId, currentBus, StringComparison.OrdinalIgnoreCase))
                {
                    currentBus = entry.BusId;
                    bool handPlaced = IsHandPlaced(entry.BusId, handPlacedTags);
                    IReadOnlyList<string> key = BusOrder.Tokens(SortablePart(entry.BusId, handPlacedTags));
                    if (handPlaced && groups.Count > 0 && CompareKeys(groups[^1].Key, key) == 0)
                    {
                        block = groups[^1].Rows;
                    }
                    else
                    {
                        block = [];
                        groups.Add(new Group(key, handPlaced, block));
                    }
                }
                block!.Add(entry);
            }

            return groups
                .OrderBy(g => g.Key, Comparer<IReadOnlyList<string>>.Create(CompareKeys))
                .ThenBy(g => g.HandPlaced)
                .SelectMany(g => g.Rows)
                .Concat(unknown)
                .ToList();
        }

        public static bool IsHandPlaced(string bus, IReadOnlyList<BusTagRule> handPlacedTags) => BusTags.Matches(bus, handPlacedTags);

        public static int MovedRows(IReadOnlyList<ConsumerEntry> before, IReadOnlyList<ConsumerEntry> after)
        {
            var position = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < before.Count; i++)
                position.TryAdd(before[i].ConsumerId, i);
            int[] sequence = after.Select(e => position.GetValueOrDefault(e.ConsumerId, -1)).Where(p => p >= 0).ToArray();
            return after.Count - LongestIncreasing(sequence).Count;
        }

        private static HashSet<int> LongestIncreasing(int[] values)
        {
            var tails = new List<int>();
            var previous = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                int lo = 0, hi = tails.Count;
                while (lo < hi)
                {
                    int mid = (lo + hi) / 2;
                    if (values[tails[mid]] < values[i])
                        lo = mid + 1;
                    else
                        hi = mid;
                }
                previous[i] = lo > 0 ? tails[lo - 1] : -1;
                if (lo == tails.Count)
                    tails.Add(i);
                else
                    tails[lo] = i;
            }

            var result = new HashSet<int>();
            for (int i = tails.Count > 0 ? tails[^1] : -1; i >= 0; i = previous[i])
                result.Add(i);
            return result;
        }

        private static string SortablePart(string bus, IReadOnlyList<BusTagRule> handPlacedTags) =>
            BusTags.MatchIndex(bus, handPlacedTags) is int index ? bus[..index] : bus;

        private static int CompareKeys(IReadOnlyList<string> a, IReadOnlyList<string> b)
        {
            for (int i = 0; i < a.Count && i < b.Count; i++)
            {
                int result = CompareToken(a[i], b[i]);
                if (result != 0)
                    return result;
            }
            return a.Count.CompareTo(b.Count);
        }

        private static int CompareToken(string a, string b)
        {
            int kindA = Rank(a[0]), kindB = Rank(b[0]);
            if (kindA != kindB)
                return kindA.CompareTo(kindB);
            if (kindA == 0)
            {
                string x = a.TrimStart('0'), y = b.TrimStart('0');
                int byLength = x.Length.CompareTo(y.Length);
                return byLength != 0 ? byLength : string.CompareOrdinal(x, y);
            }
            int length = a.Length.CompareTo(b.Length);
            return length != 0 ? length : string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static int Rank(char c) =>
            char.IsAsciiDigit(c) ? 0
            : c == '-' ? 1
            : char.IsLetter(c) ? 3
            : 2;
    }
}
