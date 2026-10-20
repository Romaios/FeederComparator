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
                    new FilledRow(1, "CONSjunc14Se", "0101", 152, null),
                    new FilledRow(2, "Unknown", "0102", null, "Not in As-Billed"),
                ]),
                new FilledFeeder("Washington Feeder", [new FilledRow(1, "B", "0201", 10.5, null)]),
            ],
            [], []);

        [Fact]
        public void Workbook_OneSheetPerFeeder_InFeederOrder()
        {
            string path = TestFiles.TempPath(".xlsx");
            new FeederExporter().WriteWorkbook(path, Result());
            using var wb = new XLWorkbook(path);
            Assert.Equal(["New York Feeder", "Washington Feeder"], wb.Worksheets.Select(w => w.Name));
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
            Assert.True(ws.Range("A1:D1").IsMerged());
            Assert.Equal(["Count", "Bus ID", "Consumer ID", "kWh (Jul 2026)"], Enumerable.Range(1, 4).Select(c => ws.Cell(2, c).GetText()));

            Assert.Equal(1, ws.Cell("A3").GetDouble());
            Assert.Equal("CONSjunc14Se", ws.Cell("B3").GetText());
            Assert.Equal(XLDataType.Text, ws.Cell("C3").DataType);
            Assert.Equal("0101", ws.Cell("C3").GetText());
            Assert.Equal(XLDataType.Number, ws.Cell("D3").DataType);
            Assert.Equal(152, ws.Cell("D3").GetDouble());

            Assert.True(ws.Cell("D4").IsEmpty());
            Assert.Equal(XLColor.FromArgb(255, 242, 204), ws.Cell("D4").Style.Fill.BackgroundColor);
            Assert.Equal(2, ws.SheetView.SplitRow);
        }

        [Fact]
        public void Separate_OneFilePerFeeder_NamedByFeederAndMonth()
        {
            string folder = TestFiles.TempFolder();
            new FeederExporter().WriteSeparate(folder, Result());
            Assert.True(File.Exists(Path.Combine(folder, "New York Feeder - Jul 2026.xlsx")));
            Assert.True(File.Exists(Path.Combine(folder, "Washington Feeder - Jul 2026.xlsx")));
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
