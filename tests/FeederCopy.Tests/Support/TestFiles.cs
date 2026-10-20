using System.Globalization;
using System.Security;
using System.Text;
using ClosedXML.Excel;

namespace FeederCopy.Tests.Support
{
    internal static class TestFiles
    {
        public static string TempPath(string extension) =>
            Path.Combine(Path.GetTempPath(), $"fc-test-{Guid.NewGuid():N}{extension}");

        public static string TempFolder()
        {
            string path = TempPath(string.Empty);
            Directory.CreateDirectory(path);
            return path;
        }

        public static string WriteSpreadsheetMl(IEnumerable<string?[]> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
            sb.AppendLine(" <Worksheet ss:Name=\"Table1\">");
            sb.AppendLine("  <Table>");
            foreach (string?[] row in rows)
            {
                sb.Append("   <Row>");
                foreach (string? cell in row)
                {
                    if (cell is null)
                    {
                        sb.Append("<Cell/>");
                        continue;
                    }
                    string type = double.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ? "Number" : "String";
                    sb.Append($"<Cell><Data ss:Type=\"{type}\">{SecurityElement.Escape(cell)}</Data></Cell>");
                }
                sb.AppendLine("</Row>");
            }
            sb.AppendLine("  </Table>");
            sb.AppendLine(" </Worksheet>");
            sb.AppendLine("</Workbook>");
            string path = TempPath(".xls");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            return path;
        }

        public static string WriteRawText(string content, string extension)
        {
            string path = TempPath(extension);
            File.WriteAllText(path, content, new UTF8Encoding(false));
            return path;
        }

        public static string WriteXlsx(IEnumerable<object?[]> rows, string? path = null) =>
            WriteXlsxSheets([("Sheet1", rows)], path);

        public static string WriteXlsxSheets(IEnumerable<(string Name, IEnumerable<object?[]> Rows)> sheets, string? path = null)
        {
            path ??= TempPath(".xlsx");
            using var workbook = new XLWorkbook();
            foreach ((string name, IEnumerable<object?[]> rows) in sheets)
            {
                IXLWorksheet sheet = workbook.AddWorksheet(name);
                int r = 1;
                foreach (object?[] row in rows)
                {
                    for (int c = 0; c < row.Length; c++)
                    {
                        sheet.Cell(r, c + 1).Value = row[c] switch
                        {
                            null => Blank.Value,
                            double d => d,
                            int i => i,
                            var o => o.ToString(),
                        };
                    }
                    r++;
                }
            }
            workbook.SaveAs(path);
            return path;
        }

        public static string WriteSpreadsheetMlSheets(IEnumerable<(string Name, IEnumerable<string?[]> Rows)> sheets)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
            foreach ((string name, IEnumerable<string?[]> rows) in sheets)
            {
                sb.AppendLine($" <Worksheet ss:Name=\"{SecurityElement.Escape(name)}\"><Table>");
                foreach (string?[] row in rows)
                {
                    sb.Append("  <Row>");
                    foreach (string? cell in row)
                        sb.Append(cell is null ? "<Cell/>" : $"<Cell><Data ss:Type=\"String\">{SecurityElement.Escape(cell)}</Data></Cell>");
                    sb.AppendLine("</Row>");
                }
                sb.AppendLine(" </Table></Worksheet>");
            }
            sb.AppendLine("</Workbook>");
            string path = TempPath(".xls");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            return path;
        }
    }
}
