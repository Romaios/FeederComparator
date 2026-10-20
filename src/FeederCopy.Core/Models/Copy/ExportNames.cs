using FeederCopy.Core.Models.Billing;

namespace FeederCopy.Core.Models.Copy
{
    public static class ExportNames
    {
        private const int MaxSheetName = 31;
        private const string Fallback = "Feeder";
        private static readonly char[] InvalidSheetChars = [':', '\\', '/', '?', '*', '[', ']'];

        public static IReadOnlyList<string> SheetNames(IReadOnlyList<string> feederNames) =>
            MakeUnique(feederNames.Select(CleanSheetName), MaxSheetName);

        public static string FeederFileName(string feederName, BillMonth month) =>
            $"{CleanFileName(feederName)} - {month}.xlsx";

        public static IReadOnlyList<string> FeederFileNames(IReadOnlyList<string> feederNames, BillMonth month) =>
            MakeUnique(feederNames.Select(CleanFileName), int.MaxValue).Select(n => $"{n} - {month}.xlsx").ToList();

        public static string WorkbookFileName(BillMonth month) => $"Feeders - {month}.xlsx";

        private static string CleanSheetName(string name)
        {
            string cleaned = new string(name.Select(c => InvalidSheetChars.Contains(c) ? '-' : c).ToArray()).Trim().Trim('\'').Trim();
            if (cleaned.Length > MaxSheetName)
                cleaned = cleaned[..MaxSheetName];
            return cleaned.Length == 0 ? Fallback : cleaned;
        }

        private static string CleanFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            string cleaned = new string(name.Select(c => invalid.Contains(c) ? '-' : c).ToArray()).Trim().TrimEnd('.');
            return cleaned.Length == 0 ? Fallback : cleaned;
        }

        private static List<string> MakeUnique(IEnumerable<string> names, int maxLength)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            foreach (string name in names)
            {
                string candidate = name;
                for (int n = 2; !used.Add(candidate); n++)
                {
                    string suffix = $" ({n})";
                    string stem = name.Length + suffix.Length > maxLength ? name[..(maxLength - suffix.Length)] : name;
                    candidate = stem + suffix;
                }
                result.Add(candidate);
            }
            return result;
        }
    }
}
