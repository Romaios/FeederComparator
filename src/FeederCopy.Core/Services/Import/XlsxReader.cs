using System.Globalization;
using ClosedXML.Excel;

namespace FeederCopy.Core.Services.Import
{
    public sealed class XlsxReader : ITableReader
    {
        public IEnumerable<IReadOnlyList<string?>> ReadRows(string path)
        {
            using var workbook = new XLWorkbook(path);
            foreach (IReadOnlyList<string?> row in Rows(workbook.Worksheet(1)))
                yield return row;
        }

        public IReadOnlyList<TableSheet> ReadSheets(string path)
        {
            using var workbook = new XLWorkbook(path);
            return workbook.Worksheets.Select(sheet => new TableSheet(sheet.Name, Rows(sheet).ToList())).ToList();
        }

        private static IEnumerable<IReadOnlyList<string?>> Rows(IXLWorksheet sheet)
        {
            int lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            for (int r = 1; r <= lastRow; r++)
            {
                IXLRow row = sheet.Row(r);
                var cells = new string?[lastColumn];
                for (int c = 1; c <= lastColumn; c++)
                    cells[c - 1] = CellText(row.Cell(c));
                yield return cells;
            }
        }

        private static string? CellText(IXLCell cell)
        {
            XLCellValue value = cell.Value;
            if (value.IsBlank)
                return null;
            if (!value.IsNumber)
                return value.IsText ? value.GetText() : value.ToString(CultureInfo.InvariantCulture);

            double number = value.GetNumber();
            string format = cell.Style.NumberFormat.Format;
            return format.Length > 1 && format.All(ch => ch == '0') && number == Math.Floor(number)
                ? number.ToString(format, CultureInfo.InvariantCulture)
                : number.ToString(CultureInfo.InvariantCulture);
        }
    }
}
