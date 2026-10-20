namespace FeederCopy.Core.Services.Import
{
    public static class HeaderIndex
    {
        public static IReadOnlyList<int> Require(IReadOnlyList<string?> header, IReadOnlyList<string> names)
        {
            var positions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Count; i++)
            {
                string name = Normalize(header[i]);
                if (name.Length > 0)
                    positions.TryAdd(name, i);
            }

            var found = new List<int>();
            var missing = new List<string>();
            foreach (string name in names)
            {
                if (positions.TryGetValue(Normalize(name), out int position))
                    found.Add(position);
                else
                    missing.Add(Normalize(name));
            }

            if (missing.Count > 0)
                throw new InvalidDataException(
                    $"Missing column(s): {string.Join(", ", missing)}. This file's columns: {string.Join(", ", positions.Keys)}. Change the column names in the Settings tab.");
            return found;
        }

        public static string? Cell(IReadOnlyList<string?> cells, int index)
        {
            if (index >= cells.Count)
                return null;
            string? value = cells[index]?.Trim();
            return string.IsNullOrEmpty(value) ? null : value;
        }

        public static bool IsBlank(IReadOnlyList<string?> cells) => cells.All(string.IsNullOrWhiteSpace);

        private static string Normalize(string? text) =>
            string.Join(' ', (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
