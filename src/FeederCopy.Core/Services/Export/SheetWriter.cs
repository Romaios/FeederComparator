using ClosedXML.Excel;

namespace FeederCopy.Core.Services.Export
{
    internal static class SheetWriter
    {
        public static void Title(IXLWorksheet sheet, string title, int columns)
        {
            sheet.Cell(1, 1).Value = title;
            sheet.Range(1, 1, 1, columns).Merge();
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;
            sheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        public static void Headers(IXLWorksheet sheet, IReadOnlyList<string> headers)
        {
            for (int c = 0; c < headers.Count; c++)
                sheet.Cell(2, c + 1).Value = headers[c];
            sheet.Range(2, 1, 2, headers.Count).Style.Font.Bold = true;
            sheet.SheetView.FreezeRows(2);
        }
    }
}
