using ClosedXML.Excel;
using FeederCopy.Core.Models.Copy;

namespace FeederCopy.Core.Services.Export
{
    public sealed class FeederExporter : IFeederExporter
    {
        public const string InactiveName = "Inactive";

        private static readonly XLColor ProblemFill = XLColor.FromArgb(255, 242, 204);

        public void WriteWorkbook(string path, CopyResult result)
        {
            using var workbook = new XLWorkbook();
            IReadOnlyList<string> sheetNames = ExportNames.SheetNames(SheetSources(result));
            for (int i = 0; i < result.Feeders.Count; i++)
                WriteSheet(workbook.AddWorksheet(sheetNames[i]), result.Feeders[i], result);
            if (HasInactive(result))
                WriteInactiveSheet(workbook.AddWorksheet(sheetNames[^1]), result);
            workbook.SaveAs(path);
        }

        public IReadOnlyList<string> SeparatePaths(string folder, CopyResult result) =>
            ExportNames.FeederFileNames(SheetSources(result), result.Month)
                .Select(name => Path.Combine(folder, name))
                .ToList();

        public void WriteSeparate(string folder, CopyResult result)
        {
            IReadOnlyList<string> paths = SeparatePaths(folder, result);
            IReadOnlyList<string> sheetNames = ExportNames.SheetNames(SheetSources(result));
            for (int i = 0; i < result.Feeders.Count; i++)
            {
                using var workbook = new XLWorkbook();
                WriteSheet(workbook.AddWorksheet(sheetNames[i]), result.Feeders[i], result);
                workbook.SaveAs(paths[i]);
            }
            if (HasInactive(result))
            {
                using var workbook = new XLWorkbook();
                WriteInactiveSheet(workbook.AddWorksheet(sheetNames[^1]), result);
                workbook.SaveAs(paths[^1]);
            }
        }

        private static bool HasInactive(CopyResult result) => result.Feeders.Any(f => f.Rows.Any(r => !r.Active));

        private static List<string> SheetSources(CopyResult result)
        {
            List<string> names = result.Feeders.Select(f => f.Name).ToList();
            if (HasInactive(result))
                names.Add(InactiveName);
            return names;
        }

        private static void WriteTitle(IXLWorksheet sheet, string title, int columns)
        {
            sheet.Cell(1, 1).Value = title;
            sheet.Range(1, 1, 1, columns).Merge();
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;
            sheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private static void WriteHeaders(IXLWorksheet sheet, string[] headers)
        {
            for (int c = 0; c < headers.Length; c++)
                sheet.Cell(2, c + 1).Value = headers[c];
            sheet.Range(2, 1, 2, headers.Length).Style.Font.Bold = true;
            sheet.SheetView.FreezeRows(2);
        }

        private static void WriteSheet(IXLWorksheet sheet, FilledFeeder feeder, CopyResult result)
        {
            WriteTitle(sheet, feeder.Name, 5);
            WriteHeaders(sheet, ["Count", "Bus ID", "Consumer ID", "Status", $"kWh ({result.Month})"]);

            int r = 3;
            foreach (FilledRow row in feeder.Rows.Where(row => row.Active))
            {
                sheet.Cell(r, 1).Value = row.Count;
                sheet.Cell(r, 2).Value = row.BusLabel;
                sheet.Cell(r, 3).SetValue(row.ConsumerId);
                sheet.Cell(r, 4).Value = 1;
                if (row.Kwh is double kwh)
                    sheet.Cell(r, 5).Value = kwh;
                if (row.Problem is not null)
                    sheet.Range(r, 4, r, 5).Style.Fill.BackgroundColor = ProblemFill;
                r++;
            }

            sheet.Column(5).Style.NumberFormat.Format = "#,##0.00";
            sheet.Columns(1, 5).AdjustToContents();
        }

        private static void WriteInactiveSheet(IXLWorksheet sheet, CopyResult result)
        {
            WriteTitle(sheet, $"Inactive consumers ({result.Month})", 4);
            WriteHeaders(sheet, ["Feeder", "Bus ID", "Consumer ID", "Status"]);

            int r = 3;
            foreach (FilledFeeder feeder in result.Feeders)
            {
                foreach (FilledRow row in feeder.Rows.Where(row => !row.Active))
                {
                    sheet.Cell(r, 1).Value = feeder.Name;
                    sheet.Cell(r, 2).Value = row.BusLabel;
                    sheet.Cell(r, 3).SetValue(row.ConsumerId);
                    sheet.Cell(r, 4).Value = 0;
                    r++;
                }
            }
            sheet.Columns(1, 4).AdjustToContents();
        }
    }
}
