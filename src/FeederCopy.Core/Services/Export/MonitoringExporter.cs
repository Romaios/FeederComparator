using ClosedXML.Excel;
using FeederCopy.Core.Models.Monitoring;

namespace FeederCopy.Core.Services.Export
{
    public sealed class MonitoringExporter : IMonitoringExporter
    {
        private const string KwhFormat = "#,##0.00";
        private const string CountFormat = "#,##0";

        private static readonly XLColor HeaderFill = XLColor.FromArgb(235, 242, 244);

        public void WriteMonitoring(string path, int year, IReadOnlyList<int> months, IReadOnlyList<MonitoringSection> sections)
        {
            using var workbook = new XLWorkbook();
            IXLWorksheet sheet = workbook.AddWorksheet("Monitoring");
            int columns = months.Count + 1;
            SheetWriter.Title(sheet, $"Monitoring {year}", Math.Max(columns, 2));

            int r = 3;
            foreach (MonitoringSection section in sections)
            {
                sheet.Cell(r, 1).Value = section.Title;
                sheet.Cell(r, 1).Style.Font.Bold = true;
                sheet.Cell(r, 1).Style.Font.FontSize = 12;
                r++;

                sheet.Cell(r, 1).Value = "Feeder";
                for (int i = 0; i < months.Count; i++)
                    sheet.Cell(r, i + 2).Value = MonitoringTable.MonthHeaders[months[i] - 1];
                IXLRange header = sheet.Range(r, 1, r, columns);
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = HeaderFill;
                r++;

                int first = r;
                foreach (FeederMonthsRow row in section.Rows)
                {
                    sheet.Cell(r, 1).Value = row.Feeder;
                    for (int i = 0; i < months.Count; i++)
                    {
                        if (row.Months[months[i] - 1] is double value)
                            sheet.Cell(r, i + 2).Value = value;
                    }
                    if (row.IsTotal)
                        sheet.Range(r, 1, r, columns).Style.Font.Bold = true;
                    r++;
                }
                if (months.Count > 0 && r > first)
                    sheet.Range(first, 2, r - 1, columns).Style.NumberFormat.Format = section.Kwh ? KwhFormat : CountFormat;
                r++;
            }

            sheet.Columns(1, columns).AdjustToContents();
            workbook.SaveAs(path);
        }

        public void WriteReport(string path, int year, IReadOnlyList<(MonitoringMeasure Measure, IReadOnlyList<FeederMonthsRow> Rows)> sheets)
        {
            using var workbook = new XLWorkbook();
            foreach ((MonitoringMeasure measure, IReadOnlyList<FeederMonthsRow> rows) in sheets)
                WriteMonths(workbook.AddWorksheet(MonitoringTable.Label(measure)), year, measure, rows);
            workbook.SaveAs(path);
        }

        private static void WriteMonths(IXLWorksheet sheet, int year, MonitoringMeasure measure, IReadOnlyList<FeederMonthsRow> rows)
        {
            List<string> headers = ["Feeder", .. MonitoringTable.MonthHeaders, MonitoringTable.TotalHeader];
            SheetWriter.Title(sheet, $"{MonitoringTable.Label(measure)} {year}", headers.Count);
            SheetWriter.Headers(sheet, headers);

            int r = 3;
            foreach (FeederMonthsRow row in rows)
            {
                sheet.Cell(r, 1).Value = row.Feeder;
                for (int m = 0; m < row.Months.Count; m++)
                {
                    if (row.Months[m] is double value)
                        sheet.Cell(r, m + 2).Value = value;
                }
                if (row.Total is double total)
                    sheet.Cell(r, headers.Count).Value = total;
                if (row.IsTotal)
                    sheet.Range(r, 1, r, headers.Count).Style.Font.Bold = true;
                r++;
            }

            sheet.Columns(2, headers.Count).Style.NumberFormat.Format = MonitoringTable.IsKwh(measure) ? KwhFormat : CountFormat;
            sheet.Columns(1, headers.Count).AdjustToContents();
        }
    }
}
