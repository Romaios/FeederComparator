using ClosedXML.Excel;
using FeederCopy.Core.Models.Copy;

namespace FeederCopy.Core.Services.Export
{
    public sealed class FeederExporter : IFeederExporter
    {
        private static readonly XLColor ProblemFill = XLColor.FromArgb(255, 242, 204);

        public void WriteWorkbook(string path, CopyResult result)
        {
            using var workbook = new XLWorkbook();
            IReadOnlyList<string> sheetNames = ExportNames.SheetNames(result.Feeders.Select(f => f.Name).ToList());
            for (int i = 0; i < result.Feeders.Count; i++)
                WriteSheet(workbook.AddWorksheet(sheetNames[i]), result.Feeders[i], result);
            workbook.SaveAs(path);
        }

        public IReadOnlyList<string> SeparatePaths(string folder, CopyResult result) =>
            ExportNames.FeederFileNames(result.Feeders.Select(f => f.Name).ToList(), result.Month)
                .Select(name => Path.Combine(folder, name))
                .ToList();

        public void WriteSeparate(string folder, CopyResult result)
        {
            IReadOnlyList<string> paths = SeparatePaths(folder, result);
            IReadOnlyList<string> sheetNames = ExportNames.SheetNames(result.Feeders.Select(f => f.Name).ToList());
            for (int i = 0; i < result.Feeders.Count; i++)
            {
                using var workbook = new XLWorkbook();
                WriteSheet(workbook.AddWorksheet(sheetNames[i]), result.Feeders[i], result);
                workbook.SaveAs(paths[i]);
            }
        }

        private static void WriteSheet(IXLWorksheet sheet, FilledFeeder feeder, CopyResult result)
        {
            sheet.Cell(1, 1).Value = feeder.Name;
            sheet.Range(1, 1, 1, 5).Merge();
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;
            sheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string[] headers = ["Count", "Bus ID", "Consumer ID", "Status", $"kWh ({result.Month})"];
            for (int c = 0; c < headers.Length; c++)
                sheet.Cell(2, c + 1).Value = headers[c];
            sheet.Range(2, 1, 2, 5).Style.Font.Bold = true;

            int r = 3;
            foreach (FilledRow row in feeder.Rows)
            {
                sheet.Cell(r, 1).Value = row.Count;
                sheet.Cell(r, 2).Value = row.BusLabel;
                sheet.Cell(r, 3).SetValue(row.ConsumerId);
                sheet.Cell(r, 4).Value = row.Status;
                if (row.Kwh is double kwh)
                    sheet.Cell(r, 5).Value = kwh;
                if (row.Problem is not null)
                    sheet.Range(r, 4, r, 5).Style.Fill.BackgroundColor = ProblemFill;
                r++;
            }

            sheet.Column(5).Style.NumberFormat.Format = "#,##0.00";
            sheet.SheetView.FreezeRows(2);
            sheet.Columns(1, 5).AdjustToContents();
        }
    }
}
