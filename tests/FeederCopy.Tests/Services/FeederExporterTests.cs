using ClosedXML.Excel;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Services.Export;

namespace FeederCopy.Tests.Services
{
    public sealed class FeederExporterTests
    {
        private static readonly BillMonth Jul = new(2026, 7);

        private static CopyResult Result() => new(Jul, "x.xls",
            [
                new FilledFeeder("New York Feeder",
                [
                    new FilledRow(1, "CONSjunc14Se", "0101", true, "Active", 152, null),
                    new FilledRow(2, "Unknown", "0102", false, "Inactive", null, null),
                    new FilledRow(2, "Unknown", "0103", true, "Duplicate in As-Billed", null, "Appears 2 times in As-Billed"),
                ]),
                new FilledFeeder("Washington Feeder", [new FilledRow(1, "B", "0201", true, "Active", 10.5, null)]),
            ],
            [], []);

        [Fact]
        public void Workbook_OneSheetPerFeeder_InFeederOrder()
        {
            string path = TestFiles.TempPath(".xlsx");
            new FeederExporter().WriteWorkbook(path, Result());
            using var wb = new XLWorkbook(path);
            Assert.Equal(["New York Feeder", "Washington Feeder", "Inactive"], wb.Worksheets.Select(w => w.Name));
        }

        [Fact]
        public void Sheet_Layout()
        {
            string path = TestFiles.TempPath(".xlsx");
            new FeederExporter().WriteWorkbook(path, Result());
            using var wb = new XLWorkbook(path);
            IXLWorksheet ws = wb.Worksheet(1);

            Assert.Equal("New York Feeder", ws.Cell("A1").GetText());
            Assert.True(ws.Cell("A1").Style.Font.Bold);
            Assert.True(ws.Range("A1:E1").IsMerged());
            Assert.Equal(["Count", "Bus ID", "Consumer ID", "Status", "kWh (Jul 2026)"], Enumerable.Range(1, 5).Select(c => ws.Cell(2, c).GetText()));

            Assert.Equal(1, ws.Cell("A3").GetDouble());
            Assert.Equal("CONSjunc14Se", ws.Cell("B3").GetText());
            Assert.Equal(XLDataType.Text, ws.Cell("C3").DataType);
            Assert.Equal("0101", ws.Cell("C3").GetText());
            Assert.Equal(XLDataType.Number, ws.Cell("D3").DataType);
            Assert.Equal(1, ws.Cell("D3").GetDouble());
            Assert.Equal(XLDataType.Number, ws.Cell("E3").DataType);
            Assert.Equal(152, ws.Cell("E3").GetDouble());

            Assert.Equal("0103", ws.Cell("C4").GetText());
            Assert.Equal(1, ws.Cell("D4").GetDouble());
            Assert.True(ws.Cell("E4").IsEmpty());
            Assert.Equal(XLColor.FromArgb(255, 242, 204), ws.Cell("D4").Style.Fill.BackgroundColor);
            Assert.Equal(XLColor.FromArgb(255, 242, 204), ws.Cell("E4").Style.Fill.BackgroundColor);
            Assert.True(ws.Cell("C5").IsEmpty());
            Assert.Equal(2, ws.SheetView.SplitRow);
        }

        [Fact]
        public void Workbook_InactiveSheet_ListsStatusZeroWithFeeder()
        {
            string path = TestFiles.TempPath(".xlsx");
            new FeederExporter().WriteWorkbook(path, Result());
            using var wb = new XLWorkbook(path);
            IXLWorksheet ws = wb.Worksheet("Inactive");
            Assert.Equal("Inactive consumers (Jul 2026)", ws.Cell("A1").GetText());
            Assert.Equal(["Feeder", "Bus ID", "Consumer ID", "Status"], Enumerable.Range(1, 4).Select(c => ws.Cell(2, c).GetText()));
            Assert.Equal(["New York Feeder", "Unknown", "0102"], Enumerable.Range(1, 3).Select(c => ws.Cell(3, c).GetText()));
            Assert.Equal(0, ws.Cell("D3").GetDouble());
            Assert.True(ws.Cell("A4").IsEmpty());
        }

        [Fact]
        public void Workbook_NoInactive_NoInactiveSheet()
        {
            string path = TestFiles.TempPath(".xlsx");
            var result = new CopyResult(Jul, "x.xls", [new FilledFeeder("F", [new FilledRow(1, "B", "0201", true, "Active", 1, null)])], [], []);
            new FeederExporter().WriteWorkbook(path, result);
            using var wb = new XLWorkbook(path);
            Assert.Equal(["F"], wb.Worksheets.Select(w => w.Name));
        }

        [Fact]
        public void Workbook_FeederNamedInactive_SheetNamesStayUnique()
        {
            string path = TestFiles.TempPath(".xlsx");
            var result = new CopyResult(Jul, "x.xls", [new FilledFeeder("Inactive", [new FilledRow(1, "B", "0201", false, "Inactive", null, null)])], [], []);
            new FeederExporter().WriteWorkbook(path, result);
            using var wb = new XLWorkbook(path);
            Assert.Equal(["Inactive", "Inactive (2)"], wb.Worksheets.Select(w => w.Name));
        }

        [Fact]
        public void Separate_OneFilePerFeeder_NamedByFeederAndMonth()
        {
            string folder = TestFiles.TempFolder();
            new FeederExporter().WriteSeparate(folder, Result());
            Assert.True(File.Exists(Path.Combine(folder, "New York Feeder - Jul 2026.xlsx")));
            Assert.True(File.Exists(Path.Combine(folder, "Washington Feeder - Jul 2026.xlsx")));
            Assert.True(File.Exists(Path.Combine(folder, "Inactive - Jul 2026.xlsx")));
            using var wb = new XLWorkbook(Path.Combine(folder, "Washington Feeder - Jul 2026.xlsx"));
            Assert.Equal("Washington Feeder", wb.Worksheet(1).Cell("A1").GetText());
        }

        [Fact]
        public void SeparatePaths_MatchesWrittenFiles()
        {
            string folder = TestFiles.TempFolder();
            var exporter = new FeederExporter();
            IReadOnlyList<string> expected = exporter.SeparatePaths(folder, Result());
            exporter.WriteSeparate(folder, Result());
            Assert.Equal(expected.Order(), Directory.GetFiles(folder).Order());
        }
    }
}
