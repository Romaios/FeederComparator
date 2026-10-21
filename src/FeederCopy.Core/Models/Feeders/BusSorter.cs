using System.Text.RegularExpressions;

namespace FeederCopy.Core.Models.Feeders
{
    public static class BusSorter
    {
        private sealed record Group(IReadOnlyList<string> Key, bool HandPlaced, List<ConsumerEntry> Rows);

        public static List<ConsumerEntry> Sort(IReadOnlyList<ConsumerEntry> entries, IReadOnlyList<string> handPlacedTags)
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

        public static IReadOnlyList<string> ParseTags(string? text) =>
            (text ?? string.Empty).Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        public static bool IsHandPlaced(string bus, IReadOnlyList<string> handPlacedTags) =>
            TagPattern(handPlacedTags) is Regex pattern && pattern.IsMatch(bus);

        public static int MovedRows(IReadOnlyList<ConsumerEntry> before, IReadOnlyList<ConsumerEntry> after)
        {
            List<(string Bus, int Rows)> beforeBlocks = Blocks(before);
            var position = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < beforeBlocks.Count; i++)
                position.TryAdd(beforeBlocks[i].Bus, i);

            List<(string Bus, int Rows)> afterBlocks = Blocks(after);
            int[] sequence = afterBlocks.Select(b => position.GetValueOrDefault(b.Bus, -1)).ToArray();
            HashSet<int> staying = LongestIncreasing(sequence);
            return afterBlocks.Where((_, i) => !staying.Contains(i)).Sum(b => b.Rows);
        }

        private static List<(string Bus, int Rows)> Blocks(IReadOnlyList<ConsumerEntry> entries)
        {
            var blocks = new List<(string Bus, int Rows)>();
            foreach (ConsumerEntry entry in entries)
            {
                if (entry.BusId is null)
                    continue;
                if (blocks.Count > 0 && string.Equals(blocks[^1].Bus, entry.BusId, StringComparison.OrdinalIgnoreCase))
                    blocks[^1] = (blocks[^1].Bus, blocks[^1].Rows + 1);
                else
                    blocks.Add((entry.BusId, 1));
            }
            return blocks;
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

        private static string SortablePart(string bus, IReadOnlyList<string> handPlacedTags) =>
            TagPattern(handPlacedTags)?.Match(bus) is { Success: true } match ? bus[..match.Index] : bus;

        private static Regex? TagPattern(IReadOnlyList<string> tags)
        {
            List<string> words = tags.Select(t => t.Trim()).Where(t => t.Length > 0).Select(Regex.Escape).ToList();
            return words.Count == 0 ? null : new Regex($"(?:{string.Join("|", words)})(?=[0-9])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

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
