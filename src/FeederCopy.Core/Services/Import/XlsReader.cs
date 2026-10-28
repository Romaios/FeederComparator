using System.Globalization;
using System.Text;
using ExcelDataReader;

namespace FeederCopy.Core.Services.Import
{
    public sealed class XlsReader : ITableReader
    {
        static XlsReader() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        public IEnumerable<IReadOnlyList<string?>> ReadRows(string path) => ReadSheets(path).FirstOrDefault()?.Rows ?? [];

        public IReadOnlyList<TableSheet> ReadSheets(string path)
        {
            using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using IExcelDataReader reader = ExcelReaderFactory.CreateBinaryReader(stream, new ExcelReaderConfiguration { FallbackEncoding = Encoding.GetEncoding(1252) });
            var sheets = new List<TableSheet>();
            do
            {
                var rows = new List<IReadOnlyList<string?>>();
                while (reader.Read())
                {
                    var cells = new string?[reader.FieldCount];
                    for (int c = 0; c < reader.FieldCount; c++)
                        cells[c] = CellText(reader.GetValue(c), reader.GetNumberFormatString(c));
                    rows.Add(cells);
                }
                sheets.Add(new TableSheet(reader.Name, rows));
            }
            while (reader.NextResult());
            return sheets;
        }

        private static string? CellText(object? value, string? format) => value switch
        {
            null or DBNull => null,
            string text => text,
            double number when format is { Length: > 1 } && format.All(ch => ch == '0') && number == Math.Floor(number) =>
                number.ToString(format, CultureInfo.InvariantCulture),
            double number => number.ToString(CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture),
        };
    }
}
