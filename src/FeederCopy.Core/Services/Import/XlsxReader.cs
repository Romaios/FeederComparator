using System.Globalization;
using ClosedXML.Excel;

namespace FeederCopy.Core.Services.Import
{
    public sealed class XlsxReader : ITableReader
    {
        public IEnumerable<IReadOnlyList<string?>> ReadRows(string path)
        {
            using var workbook = new XLWorkbook(path);
            IXLWorksheet sheet = workbook.Worksheet(1);
            int lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            foreach (IXLRow row in sheet.RowsUsed())
            {
                var cells = new string?[lastColumn];
                for (int c = 1; c <= lastColumn; c++)
                    cells[c - 1] = CellText(row.Cell(c).Value);
                yield return cells;
            }
        }

        private static string? CellText(XLCellValue value) =>
            value.IsBlank ? null
            : value.IsNumber ? value.GetNumber().ToString(CultureInfo.InvariantCulture)
            : value.IsText ? value.GetText()
            : value.ToString(CultureInfo.InvariantCulture);
    }
}
