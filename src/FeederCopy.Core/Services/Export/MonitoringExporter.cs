using ClosedXML.Excel;
using FeederCopy.Core.Models.Monitoring;

namespace FeederCopy.Core.Services.Export
{
    public sealed class MonitoringExporter : IMonitoringExporter
    {
        private const string KwhFormat = "#,##0.00";
        private const string CountFormat = "#,##0";

        public void WriteSummary(string path, int year, IReadOnlyList<MonthSummaryRow> rows)
        {
            using var workbook = new XLWorkbook();
            IXLWorksheet sheet = workbook.AddWorksheet("Monitoring");
            SheetWriter.Title(sheet, $"Monitoring {year}", 5);
            SheetWriter.Headers(sheet, ["Month", "Feeder", "Total kWh", "Active", "Inactive"]);

            int r = 3;
            foreach (MonthSummaryRow row in rows)
            {
                sheet.Cell(r, 1).Value = row.Month.ToString();
                sheet.Cell(r, 2).Value = row.Feeder;
                sheet.Cell(r, 3).Value = row.TotalKwh;
                sheet.Cell(r, 4).Value = row.Active;
                sheet.Cell(r, 5).Value = row.Inactive;
                if (row.IsTotal)
                    sheet.Range(r, 1, r, 5).Style.Font.Bold = true;
                r++;
            }

            sheet.Column(3).Style.NumberFormat.Format = KwhFormat;
            sheet.Columns(4, 5).Style.NumberFormat.Format = CountFormat;
            sheet.Columns(1, 5).AdjustToContents();
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
            List<string> headers = ["Feeder", .. MonitoringTable.MonthHeaders, MonitoringTable.TotalHeader(measure)];
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
