using FeederCopy.Core.Models.Settings;

namespace FeederCopy.Core.Services.Import
{
    public static class HeaderIndex
    {
        public static IReadOnlyList<int> Require(IReadOnlyList<string?> header, IReadOnlyList<string> columns)
        {
            var positions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Count; i++)
            {
                string name = ColumnNames.Normalize(header[i]);
                if (name.Length > 0)
                    positions.TryAdd(name, i);
            }

            var found = new List<int>();
            var missing = new List<string>();
            foreach (string column in columns)
            {
                IReadOnlyList<string> names = ColumnNames.Split(column);
                int? position = names.Select(n => positions.TryGetValue(n, out int p) ? p : (int?)null).FirstOrDefault(p => p is not null);
                if (position is int p)
                    found.Add(p);
                else
                    missing.Add(string.Join(" or ", names));
            }

            if (missing.Count > 0)
                throw new InvalidDataException(
                    $"Missing column(s): {string.Join(", ", missing)}. This file's columns: {string.Join(", ", positions.Keys)}. Add this file's column name in the Settings tab (several names can be listed, separated by commas).");
            return found;
        }

        public static int? Find(IReadOnlyList<string?> header, string column)
        {
            foreach (string wanted in ColumnNames.Split(column))
            {
                for (int i = 0; i < header.Count; i++)
                {
                    if (string.Equals(ColumnNames.Normalize(header[i]), wanted, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }
            return null;
        }

        public static string? Cell(IReadOnlyList<string?> cells, int index)
        {
            if (index >= cells.Count)
                return null;
            string? value = cells[index]?.Trim();
            return string.IsNullOrEmpty(value) ? null : value;
        }

        public static bool IsBlank(IReadOnlyList<string?> cells) => cells.All(string.IsNullOrWhiteSpace);
    }
}
