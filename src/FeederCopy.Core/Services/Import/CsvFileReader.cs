using System.Globalization;
using CsvHelper;

namespace FeederCopy.Core.Services.Import
{
    public sealed class CsvFileReader : ITableReader
    {
        public IEnumerable<IReadOnlyList<string?>> ReadRows(string path)
        {
            using var reader = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
            using var parser = new CsvParser(reader, CultureInfo.InvariantCulture);
            while (parser.Read())
                yield return parser.Record ?? [];
        }

        public IReadOnlyList<TableSheet> ReadSheets(string path) =>
            [new TableSheet(Path.GetFileNameWithoutExtension(path), ReadRows(path).ToList())];
    }
}
