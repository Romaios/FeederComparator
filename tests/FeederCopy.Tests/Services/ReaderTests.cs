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
        public void Csv_ReadsRows()
        {
            string path = TestFiles.WriteRawText("Bus ID,Consumer ID\nA,c1\n", ".csv");
            List<IReadOnlyList<string?>> rows = TableReaderFactory.For(path).ReadRows(path).ToList();
            Assert.Equal(2, rows.Count);
            Assert.Equal(["A", "c1"], rows[1]);
        }

        [Fact]
        public void Factory_BinaryXls_ThrowsSaveAsHint()
        {
            string path = TestFiles.TempPath(".xls");
            File.WriteAllBytes(path, [0xD0, 0xCF, 0x11, 0xE0, 0, 0, 0, 0]);
            var ex = Assert.Throws<InvalidDataException>(() => TableReaderFactory.For(path));
            Assert.Contains("Save As", ex.Message);
        }

        [Fact]
        public void Factory_UnknownFormat_Throws()
        {
            string path = TestFiles.WriteRawText("hello", ".txt");
            var ex = Assert.Throws<InvalidDataException>(() => TableReaderFactory.For(path));
            Assert.Equal("Unsupported file format. Expected an .xls XML spreadsheet, an .xlsx workbook, or a .csv file.", ex.Message);
        }
    }
}
