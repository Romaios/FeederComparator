using System.Text;

namespace FeederCopy.Core.Models.Feeders
{
    public static class BusOrder
    {
        private enum Kind
        {
            Digit,
            Upper,
            Lower,
            Other,
        }

        public static int? InsertIndex(IReadOnlyList<string> blocks, string bus)
        {
            IReadOnlyList<string> tokens = Tokens(MatchCasing(bus, blocks));
            if (tokens.Count < 2)
                return null;

            List<IReadOnlyList<string>> blockTokens = blocks.Select(Tokens).ToList();
            int level = tokens.Count - 1;
            IReadOnlyList<string> parent = tokens.Take(level).ToList();
            string last = tokens[level];

            var siblings = blockTokens
                .Select((t, i) => (Tokens: t, Index: i))
                .Where(b => b.Tokens.Count > level && StartsWith(b.Tokens, parent) && SameKind(b.Tokens[level], last))
                .GroupBy(b => b.Tokens[level], StringComparer.OrdinalIgnoreCase)
                .Select(g => (Key: g.Key, First: g.Min(b => b.Index)))
                .ToList();

            var lower = siblings.Where(s => CompareToken(s.Key, last) < 0).ToList();
            if (lower.Count > 0)
            {
                string previous = lower.Select(s => s.Key).Aggregate((a, b) => CompareToken(a, b) >= 0 ? a : b);
                return SubtreeEnd(blockTokens, parent.Append(previous).ToList()) + 1;
            }

            var higher = siblings.Where(s => CompareToken(s.Key, last) > 0).ToList();
            if (higher.Count > 0)
            {
                string next = higher.Select(s => s.Key).Aggregate((a, b) => CompareToken(a, b) <= 0 ? a : b);
                return higher.First(s => string.Equals(s.Key, next, StringComparison.OrdinalIgnoreCase)).First;
            }

            for (int n = level; n >= 2; n--)
            {
                int end = SubtreeEnd(blockTokens, tokens.Take(n).ToList());
                if (end >= 0)
                    return end + 1;
            }
            return null;
        }

        public static IReadOnlyList<string> Tokens(string bus)
        {
            var tokens = new List<string>();
            var current = new StringBuilder();
            Kind? currentKind = null;
            foreach (char c in bus)
            {
                Kind kind = KindOf(c);
                if (current.Length > 0 && (kind != currentKind || kind == Kind.Other))
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
                current.Append(c);
                currentKind = kind;
            }
            if (current.Length > 0)
                tokens.Add(current.ToString());
            return tokens;
        }

        private static string MatchCasing(string bus, IReadOnlyList<string> blocks)
        {
            string? best = null;
            int bestLength = 0;
            foreach (string block in blocks)
            {
                int shared = 0;
                while (shared < bus.Length && shared < block.Length && char.ToUpperInvariant(bus[shared]) == char.ToUpperInvariant(block[shared]))
                    shared++;
                if (shared > bestLength)
                {
                    bestLength = shared;
                    best = block;
                }
            }
            return best is null ? bus : best[..bestLength] + bus[bestLength..];
        }

        private static Kind KindOf(char c) =>
            char.IsAsciiDigit(c) ? Kind.Digit
            : char.IsUpper(c) ? Kind.Upper
            : char.IsLetter(c) ? Kind.Lower
            : Kind.Other;

        private static bool SameKind(string a, string b)
        {
            Kind x = KindOf(a[0]), y = KindOf(b[0]);
            return x == y || (x is Kind.Upper or Kind.Lower && y is Kind.Upper or Kind.Lower);
        }

        private static int CompareToken(string a, string b)
        {
            if (char.IsAsciiDigit(a[0]) && char.IsAsciiDigit(b[0]))
            {
                string x = a.TrimStart('0'), y = b.TrimStart('0');
                int byLength = x.Length.CompareTo(y.Length);
                return byLength != 0 ? byLength : string.CompareOrdinal(x, y);
            }
            int length = a.Length.CompareTo(b.Length);
            return length != 0 ? length : string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static bool StartsWith(IReadOnlyList<string> tokens, IReadOnlyList<string> prefix)
        {
            if (tokens.Count < prefix.Count)
                return false;
            for (int i = 0; i < prefix.Count; i++)
            {
                if (!string.Equals(tokens[i], prefix[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        private static int SubtreeEnd(IReadOnlyList<IReadOnlyList<string>> blockTokens, IReadOnlyList<string> root)
        {
            int end = -1;
            for (int i = 0; i < blockTokens.Count; i++)
            {
                if (StartsWith(blockTokens[i], root))
                    end = i;
            }
            return end;
        }
    }
}
