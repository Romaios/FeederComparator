using FeederCopy.Core.Models.Import;
using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Services.Import;

namespace FeederCopy.Tests.Services
{
    public sealed class ReaderTests
    {
        [Fact]
        public void SpreadsheetMl_HonoursSsIndex()
        {
            string xml = "<?xml version=\"1.0\"?><Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">"
                + "<Worksheet ss:Name=\"T\"><Table><Row><Cell><Data ss:Type=\"String\">a</Data></Cell><Cell ss:Index=\"3\"><Data ss:Type=\"String\">c</Data></Cell></Row></Table></Worksheet></Workbook>";
            string path = TestFiles.WriteRawText(xml, ".xls");
            List<IReadOnlyList<string?>> rows = TableReaderFactory.For(path).ReadRows(path).ToList();
            Assert.Equal(["a", null, "c"], rows[0]);
        }

        [Fact]
        public void Xlsx_NumericCell_NoDecimalSuffix()
        {
            string path = TestFiles.WriteXlsx([[1010001d]]);
            Assert.Equal("1010001", TableReaderFactory.For(path).ReadRows(path).Single()[0]);
        }

        [Fact]
        public void Xlsx_TextCell_KeepsLeadingZeros()
        {
            string path = TestFiles.WriteXlsx([["0101-0001"], ["0101"]]);
            List<IReadOnlyList<string?>> rows = TableReaderFactory.For(path).ReadRows(path).ToList();
            Assert.Equal("0101-0001", rows[0][0]);
            Assert.Equal("0101", rows[1][0]);
        }

        [Fact]
        public void Xlsx_NumberWithZeroPaddedFormat_KeepsLeadingZeros()
        {
            string path = TestFiles.TempPath(".xlsx");
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Abuyog");
                sheet.Cell(1, 1).Value = 900001;
                sheet.Cell(1, 1).Style.NumberFormat.Format = "0000000";
                sheet.Cell(2, 1).Value = 1234.5;
                sheet.Cell(2, 1).Style.NumberFormat.Format = "#,##0.00";
                workbook.SaveAs(path);
            }
            List<IReadOnlyList<string?>> rows = TableReaderFactory.For(path).ReadSheets(path)[0].Rows.ToList();
            Assert.Equal("0900001", rows[0][0]);
            Assert.Equal("1234.5", rows[1][0]);
        }

        [Fact]
        public void Xlsx_ReadSheets_KeepsRowNumbersAcrossBlankRows()
        {
            string path = TestFiles.WriteXlsxSheets([("A", [["h"], [null], ["x"]]), ("B", [["y"]])]);
            IReadOnlyList<TableSheet> sheets = TableReaderFactory.For(path).ReadSheets(path);
            Assert.Equal(["A", "B"], sheets.Select(s => s.Name));
            Assert.Equal("x", sheets[0].Rows[2][0]);
        }

        [Fact]
        public void Csv_ReadsRows()
        {
            string path = TestFiles.WriteRawText("Bus ID,Consumer ID\nA,c1\n", ".csv");
            List<IReadOnlyList<string?>> rows = TableReaderFactory.For(path).ReadRows(path).ToList();
            Assert.Equal(2, rows.Count);
            Assert.Equal(["A", "c1"], rows[1]);
        }

        private static string OldXls => Path.Combine(AppContext.BaseDirectory, "TestData", "feeder-list-old.xls");

        [Fact]
        public void BinaryXls_ReadsEverySheetKeepingLeadingZeros()
        {
            IReadOnlyList<TableSheet> sheets = TableReaderFactory.For(OldXls).ReadSheets(OldXls);
            Assert.IsType<XlsReader>(TableReaderFactory.For(OldXls));
            Assert.Equal(["Washington", "Brooklyn"], sheets.Select(s => s.Name));
            Assert.Equal(["Bus ID", "Consumer ID", "kWh"], sheets[0].Rows[0]);
            Assert.Equal(["BUS-1", "0101001", "12.5"], sheets[0].Rows[1]);
            Assert.Equal(["BUS-1", "0101002", null], sheets[0].Rows[2]);
            Assert.Equal(["Ñipa bus", "101003", "7"], sheets[0].Rows[3]);
            Assert.Equal(["B", "0202001"], sheets[1].Rows[1]);
        }

        [Fact]
        public void BinaryXls_ReadRows_IsTheFirstSheet()
        {
            List<IReadOnlyList<string?>> rows = TableReaderFactory.For(OldXls).ReadRows(OldXls).ToList();
            Assert.Equal(4, rows.Count);
            Assert.Equal("0101001", rows[1][1]);
        }

        [Fact]
        public void BinaryXls_FeederWorkbookImports()
        {
            ParsedWorkbook workbook = FeederListParser.ReadWorkbook(OldXls, new FeederColumns { BusId = "Bus ID", ConsumerId = "Consumer ID", Status = "Status" });
            Assert.Empty(workbook.Problems);
            Assert.Equal(["Washington", "Brooklyn"], workbook.Feeders.Select(f => f.Name));
            Assert.Equal(["0101001", "0101002", "101003"], workbook.Feeders[0].Entries.Select(e => e.ConsumerId));
        }

        [Fact]
        public void Factory_UnknownFormat_Throws()
        {
            string path = TestFiles.WriteRawText("hello", ".txt");
            var ex = Assert.Throws<InvalidDataException>(() => TableReaderFactory.For(path));
            Assert.Equal("Unsupported file format. Expected an .xls or .xlsx workbook, an .xls XML spreadsheet, or a .csv file.", ex.Message);
        }
    }
}
