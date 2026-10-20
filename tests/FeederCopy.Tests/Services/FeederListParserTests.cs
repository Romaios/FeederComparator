using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Import;
using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Services.Import;

namespace FeederCopy.Tests.Services
{
    public sealed class FeederListParserTests
    {
        private static readonly object?[] Header = ["Count", "From\nBus ID", "To  \nCustomer ID"];

        private static ParsedWorkbook Parse(params (string Name, object?[][] Rows)[] sheets) =>
            FeederListParser.ReadWorkbook(TestFiles.WriteXlsxSheets(sheets.Select(s => (s.Name, (IEnumerable<object?[]>)s.Rows))), FeederColumns.Defaults);

        private static ParsedWorkbook One(params object?[][] rows) => Parse(("Abuyog", [Header, .. rows]));

        [Fact]
        public void ReadWorkbook_OneFeederPerSheet_NamedBySheet()
        {
            ParsedWorkbook result = Parse(("Abuyog", [Header, [1, "A", "c1"]]), ("Bacon", [Header, [1, "B", "c2"], [2, "B", "c3"]]));
            Assert.Empty(result.Problems);
            Assert.Equal(["Abuyog", "Bacon"], result.Feeders.Select(f => f.Name));
            Assert.Equal([new ConsumerEntry("B", "c2"), new ConsumerEntry("B", "c3")], result.Feeders[1].Entries);
        }

        [Fact]
        public void ReadWorkbook_TemplateHeadersWithLineBreaks_Match()
        {
            ParsedWorkbook result = One([1, "AFSEC4", "0900001"]);
            Assert.Empty(result.Problems);
            Assert.Equal([new ConsumerEntry("AFSEC4", "0900001")], result.Feeders.Single().Entries);
        }

        [Fact]
        public void ReadWorkbook_CountColumnOptional_AnyOrder()
        {
            ParsedWorkbook result = Parse(("Abuyog", [["To Customer ID", "From Bus ID"], ["c1", "A"]]));
            Assert.Equal([new ConsumerEntry("A", "c1")], result.Feeders.Single().Entries);
        }

        [Fact]
        public void ReadWorkbook_ArrangesIntoBusBlocks()
        {
            ParsedWorkbook result = One([1, "A", "c1"], [2, "B", "c2"], [3, "A", "c3"]);
            Assert.Equal(["c1", "c3", "c2"], result.Feeders.Single().Entries.Select(e => e.ConsumerId));
        }

        [Fact]
        public void ReadWorkbook_BlankRowsSkipped()
        {
            ParsedWorkbook result = One([1, "A", "c1"], [null, null, null], [2, "A", "c2"]);
            Assert.Equal(2, result.Feeders.Single().Entries.Count);
            Assert.Empty(result.Notes);
        }

        [Fact]
        public void ReadWorkbook_BusWithoutConsumer_Note()
        {
            ParsedWorkbook result = One([1, "A", "c1"], [2, "A", null]);
            Assert.Equal(["Abuyog row 3: Bus ID without a consumer ID; skipped"], result.Notes);
            Assert.Equal(1, result.Feeders.Single().SkippedRows);
        }

        [Fact]
        public void ReadWorkbook_ConsumerWithoutBus_ImportsAsUnknown()
        {
            ParsedWorkbook result = One([1, null, "c1"], [2, "A", "c2"]);
            Assert.Equal([new ConsumerEntry("A", "c2"), new ConsumerEntry(null, "c1")], result.Feeders.Single().Entries);
        }

        [Fact]
        public void ReadWorkbook_DuplicateInSheet_KeepsFirstWithNote()
        {
            ParsedWorkbook result = One([1, "A", "c0"], [2, "A", "c1"], [3, "B", "c2"], [4, "B", "c3"], [5, "B", "c4"], [6, "C", "c1"]);
            Assert.Empty(result.Problems);
            Assert.Equal(["Consumer ID c1 appears more than once (Abuyog row 3, Abuyog row 7); kept Abuyog row 3."], result.Notes);
            Assert.Equal(new ConsumerEntry("A", "c1"), result.Feeders.Single().Entries.Single(e => e.ConsumerId == "c1"));
        }

        [Fact]
        public void ReadWorkbook_DuplicateAcrossSheets_KeepsFirstWithNote()
        {
            ParsedWorkbook result = Parse(("Abuyog", [Header, [1, "A", "c1"]]), ("Bacon", [Header, [1, "B", "c1"], [2, "B", "c2"]]));
            Assert.Empty(result.Problems);
            Assert.Equal(["Consumer ID c1 appears more than once (Abuyog row 2, Bacon row 2); kept Abuyog row 2."], result.Notes);
            Assert.Equal([new ConsumerEntry("A", "c1")], result.Feeders[0].Entries);
            Assert.Equal([new ConsumerEntry("B", "c2")], result.Feeders[1].Entries);
        }

        private static readonly object?[] StatusHeader = ["Count", "From Bus ID", "To Customer ID", "Status"];

        [Fact]
        public void ReadWorkbook_StatusColumn_SetsActive()
        {
            ParsedWorkbook result = Parse(("Abuyog", [StatusHeader, [1, "A", "c1", "1"], [2, "A", "c2", "0"], [3, "A", "c3", null]]));
            Assert.Equal([new ConsumerEntry("A", "c1"), new ConsumerEntry("A", "c2", false), new ConsumerEntry("A", "c3")], result.Feeders.Single().Entries);
            Assert.Empty(result.Problems);
        }

        [Fact]
        public void ReadWorkbook_StatusNumericCells()
        {
            ParsedWorkbook result = Parse(("Abuyog", [StatusHeader, [1, "A", "c1", 1], [2, "A", "c2", 0]]));
            Assert.Equal([true, false], result.Feeders.Single().Entries.Select(e => e.Active));
        }

        [Fact]
        public void ReadWorkbook_StatusInvalid_Problem()
        {
            ParsedWorkbook result = Parse(("Abuyog", [StatusHeader, [1, "A", "c1", "x"]]));
            Assert.Contains("Sheet Abuyog row 2: status 'x' must be 1 or 0.", result.Problems);
        }

        [Fact]
        public void ReadWorkbook_NoStatusColumn_AllActive()
        {
            ParsedWorkbook result = One([1, "A", "c1"]);
            Assert.True(result.Feeders.Single().Entries.Single().Active);
        }

        [Fact]
        public void ReadWorkbook_DuplicateOneActive_KeepsActiveDropsInactive()
        {
            ParsedWorkbook result = Parse(("Bacon", [StatusHeader, [1, "B", "c1", 1]]), ("Sorsogon", [StatusHeader, [1, "S", "c1", 0], [2, "S", "c2", 1]]));
            Assert.Empty(result.Problems);
            Assert.Equal([new ConsumerEntry("B", "c1")], result.Feeders[0].Entries);
            Assert.Equal([new ConsumerEntry("S", "c2")], result.Feeders[1].Entries);
            Assert.Equal(["Consumer ID c1: kept the active one (Bacon row 2); dropped the inactive copy (Sorsogon row 2)."], result.Notes);
        }

        [Fact]
        public void ReadWorkbook_DuplicateSameSheetOneActive_KeepsActive()
        {
            ParsedWorkbook result = Parse(("Bacon", [StatusHeader, [1, "B", "c1", 0], [2, "B", "c1", 1]]));
            Assert.Empty(result.Problems);
            Assert.Equal([new ConsumerEntry("B", "c1")], result.Feeders.Single().Entries);
        }

        [Fact]
        public void ReadWorkbook_DuplicateAllInactive_KeepsFirst()
        {
            ParsedWorkbook result = Parse(("Abuyog", [StatusHeader, [1, "A", "c1", 0]]), ("Bacon", [StatusHeader, [1, "B", "c1", 0], [2, "B", "c2", 1]]));
            Assert.Empty(result.Problems);
            Assert.Equal([new ConsumerEntry("A", "c1", false)], result.Feeders[0].Entries);
            Assert.Equal(["Consumer ID c1 is inactive in Abuyog row 2, Bacon row 2; kept Abuyog row 2."], result.Notes);
        }

        [Fact]
        public void ReadWorkbook_SheetWithoutColumns_SkippedWithNote()
        {
            ParsedWorkbook result = Parse(("Abuyog", [Header, [1, "A", "c1"]]), ("Summary", [["Feeder", "Total"], ["Abuyog", 1]]));
            Assert.Equal(["Abuyog"], result.Feeders.Select(f => f.Name));
            Assert.Equal(["Sheet Summary skipped: it has no From Bus ID and To Customer ID columns."], result.Notes);
            Assert.Empty(result.Problems);
        }

        [Fact]
        public void ReadWorkbook_EmptySheet_SkippedSilently()
        {
            ParsedWorkbook result = Parse(("Abuyog", [Header, [1, "A", "c1"]]), ("Sheet2", []));
            Assert.Single(result.Feeders);
            Assert.Empty(result.Notes);
        }

        [Fact]
        public void ReadWorkbook_SheetWithNoConsumers_Note()
        {
            ParsedWorkbook result = Parse(("Abuyog", [Header, [1, "A", "c1"]]), ("Bacon", [Header]));
            Assert.Equal(["Sheet Bacon skipped: no consumers."], result.Notes);
        }

        [Fact]
        public void ReadWorkbook_NoFeederSheets_ProblemNamesMissingColumns()
        {
            ParsedWorkbook result = Parse(("Summary", [["Count", "Consumer"], [1, "c1"]]));
            Assert.Empty(result.Feeders);
            Assert.Contains("Missing column(s): From Bus ID, To Customer ID", result.Problems.Single());
            Assert.Contains("Settings tab", result.Problems.Single());
        }

        [Fact]
        public void ReadWorkbook_EmptyFile_Problem()
        {
            ParsedWorkbook result = FeederListParser.ReadWorkbook(TestFiles.WriteRawText("", ".csv"), FeederColumns.Defaults);
            Assert.Equal(["The file is empty."], result.Problems);
        }

        [Fact]
        public void ReadWorkbook_Csv_OneFeederNamedByFile()
        {
            string folder = TestFiles.TempFolder();
            string path = Path.Combine(folder, "Gubat.csv");
            File.WriteAllText(path, "Count,From Bus ID,To Customer ID\n1,A,c1\n");
            ParsedWorkbook result = FeederListParser.ReadWorkbook(path, FeederColumns.Defaults);
            Assert.Equal(["Gubat"], result.Feeders.Select(f => f.Name));
        }

        [Fact]
        public void ReadWorkbook_SpreadsheetMl_AllWorksheets()
        {
            string?[] header = ["Count", "From Bus ID", "To Customer ID"];
            string path = TestFiles.WriteSpreadsheetMlSheets([("Pilar", [header, ["1", "A", "c1"]]), ("Gubat", [header, ["1", "B", "c2"]])]);
            ParsedWorkbook result = FeederListParser.ReadWorkbook(path, FeederColumns.Defaults);
            Assert.Equal(["Pilar", "Gubat"], result.Feeders.Select(f => f.Name));
        }

        [Fact]
        public void ReadWorkbook_LockedFile_ProblemCloseInExcel()
        {
            string path = TestFiles.WriteXlsx([Header, [1, "A", "c1"]]);
            using var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            ParsedWorkbook result = FeederListParser.ReadWorkbook(path, FeederColumns.Defaults);
            Assert.Contains("close it and try again", result.Problems.Single());
        }

        [Fact]
        public void Defaults_MatchTemplateHeadings()
        {
            Assert.Equal("From Bus ID", FeederColumns.Defaults.BusId);
            Assert.Equal("To Customer ID", FeederColumns.Defaults.ConsumerId);
        }
    }
}
