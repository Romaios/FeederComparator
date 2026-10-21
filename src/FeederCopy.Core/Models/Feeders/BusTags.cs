using System.Text.RegularExpressions;

namespace FeederCopy.Core.Models.Feeders
{
    public sealed record BusTagRule(string? Code, string Tag, bool Exclude = false);

    public sealed record BusTagRow(string Feeder, string Code, string Tag, int Buses, string Example, bool HandPlaced);

    public static partial class BusTags
    {
        public static string Code(string bus)
        {
            int digit = bus.IndexOfAny("0123456789".ToCharArray());
            return digit < 0 ? bus : bus[..digit];
        }

        public static IReadOnlyList<BusTagRule> ParseRules(string? text) =>
            (text ?? string.Empty)
                .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(entry =>
                {
                    bool exclude = entry.StartsWith('-');
                    string body = exclude ? entry[1..] : entry;
                    int colon = body.LastIndexOf(':');
                    return colon > 0
                        ? new BusTagRule(body[..colon].Trim(), body[(colon + 1)..].Trim(), exclude)
                        : new BusTagRule(null, body.Trim(), exclude);
                })
                .Where(rule => rule.Tag.Length > 0)
                .ToList();

        public static string ToSetting(IEnumerable<BusTagRow> rows, IReadOnlyList<BusTagRule> previous)
        {
            List<string> defaults = previous
                .Where(r => r.Code is null && !r.Exclude)
                .Select(r => r.Tag)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var entries = new List<string>(defaults);
            foreach (BusTagRow row in rows.DistinctBy(r => (r.Code.ToUpperInvariant(), r.Tag.ToUpperInvariant())))
            {
                bool byDefault = defaults.Contains(row.Tag, StringComparer.OrdinalIgnoreCase);
                if (row.HandPlaced && !byDefault)
                    entries.Add($"{row.Code}:{row.Tag}");
                else if (!row.HandPlaced && byDefault)
                    entries.Add($"-{row.Code}:{row.Tag}");
            }
            return string.Join(", ", entries);
        }

        public static bool Matches(string bus, IReadOnlyList<BusTagRule> rules) => Find(bus, rules) is not null;

        public static string? MatchedTag(string bus, IReadOnlyList<BusTagRule> rules) => Find(bus, rules)?.Value;

        public static int? MatchIndex(string bus, IReadOnlyList<BusTagRule> rules) => Find(bus, rules)?.Index;

        public static IReadOnlyList<BusTagRow> Discover(IEnumerable<(string Feeder, string Bus)> buses, IReadOnlyList<BusTagRule> rules)
        {
            var found = new List<(string Feeder, string Code, string Run, string Bus)>();
            foreach ((string feeder, string bus) in buses.DistinctBy(b => (b.Feeder.ToUpperInvariant(), b.Bus.ToUpperInvariant())))
            {
                string code = Code(bus);
                foreach (Match run in TagRun().Matches(bus, code.Length))
                    found.Add((feeder, code, run.Value, bus));
            }

            var lowercaseTags = new HashSet<string>(
                found.Select(f => LowercaseTail().Match(f.Run)).Where(m => m.Success).Select(m => m.Value),
                StringComparer.OrdinalIgnoreCase);

            return found
                .Select(f => (f.Feeder, f.Code, Tag: TagOf(f.Run, lowercaseTags), f.Bus))
                .Where(f => f.Tag is not null)
                .GroupBy(f => (Feeder: f.Feeder.ToUpperInvariant(), Code: f.Code.ToUpperInvariant(), Tag: f.Tag!.ToUpperInvariant()))
                .Select(g =>
                {
                    var first = g.First();
                    int count = g.Select(f => f.Bus).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                    return new BusTagRow(first.Feeder, first.Code, first.Tag!, count, first.Bus, IsRuled(first.Code, first.Tag!, rules));
                })
                .OrderBy(r => r.Feeder, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.Code, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(r => r.Buses)
                .ThenBy(r => r.Tag, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsRuled(string code, string tag, IReadOnlyList<BusTagRule> rules) =>
            TagsFor(code, rules).Contains(tag, StringComparer.OrdinalIgnoreCase);

        private static List<string> TagsFor(string code, IReadOnlyList<BusTagRule> rules)
        {
            bool SameCode(BusTagRule r) => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase);
            var excluded = rules.Where(r => r.Exclude && SameCode(r)).Select(r => r.Tag).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return rules
                .Where(r => !r.Exclude && (r.Code is null || SameCode(r)))
                .Select(r => r.Tag)
                .Where(t => !excluded.Contains(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string? TagOf(string run, HashSet<string> lowercaseTags)
        {
            Match lower = LowercaseTail().Match(run);
            if (lower.Success)
                return lower.Value;
            for (int start = 0; start <= run.Length - 2; start++)
            {
                if (lowercaseTags.Contains(run[start..]))
                    return run[start..];
            }
            return run.Length >= 2 ? run : null;
        }

        private static Match? Find(string bus, IReadOnlyList<BusTagRule> rules)
        {
            string code = Code(bus);
            List<string> tags = TagsFor(code, rules)
                .Select(Regex.Escape)
                .OrderByDescending(t => t.Length)
                .ToList();
            if (tags.Count == 0)
                return null;
            var pattern = new Regex($"(?:{string.Join("|", tags)})(?=[0-9])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            Match match = pattern.Match(bus, code.Length);
            return match.Success ? match : null;
        }

        [GeneratedRegex("[A-Za-z]+(?=[0-9])")]
        private static partial Regex TagRun();

        [GeneratedRegex("[a-z]+$")]
        private static partial Regex LowercaseTail();
    }
}
