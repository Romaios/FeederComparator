namespace FeederCopy.Core.Models.Settings
{
    public static class ColumnNames
    {
        public static IReadOnlyList<string> Split(string? names) =>
            (names ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Normalize)
                .Where(n => n.Length > 0)
                .ToList();

        public static bool Overlap(string? a, string? b) =>
            Split(a).Intersect(Split(b), StringComparer.OrdinalIgnoreCase).Any();

        public static string Normalize(string? text) =>
            string.Join(' ', (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
