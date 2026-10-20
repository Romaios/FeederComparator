using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Import;
using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Services.Import;

namespace FeederCopy.Tests.Services
{
    public sealed class FeederListParserTests
    {
        private static readonly object?[] Header = ["Count", "Bus ID", "Consumer ID"];

        private static ParsedFeederList Parse(params object?[][] rows) =>
            FeederListParser.Read(TestFiles.WriteXlsx(rows), FeederColumns.Defaults);

        [Fact]
        public void Read_CountColumnIgnoredAndOptional()
        {
            ParsedFeederList result = Parse(["Consumer ID", "Bus ID"], ["c1", "A"], ["c2", "B"]);
            Assert.Null(result.FatalError);
            Assert.Equal([new ConsumerEntry("A", "c1"), new ConsumerEntry("B", "c2")], result.Entries);
        }

        [Fact]
        public void Read_ArrangesIntoBusBlocks()
        {
            ParsedFeederList result = Parse(Header, [1, "A", "c1"], [2, "B", "c2"], [3, "A", "c3"]);
            Assert.Equal(["c1", "c3", "c2"], result.Entries.Select(e => e.ConsumerId));
        }

        [Fact]
        public void Read_BlankRowsSkipped()
        {
            ParsedFeederList result = Parse(Header, [1, "A", "c1"], [null, null, null], [2, "A", "c2"]);
            Assert.Equal(2, result.Entries.Count);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void Read_BusWithoutConsumer_Warning()
        {
            ParsedFeederList result = Parse(Header, [1, "A", "c1"], [2, "A", null]);
            Assert.Equal([new RowError(3, "Bus ID without a consumer ID; skipped")], result.Warnings);
            Assert.Single(result.Entries);
        }

        [Fact]
        public void Read_ConsumerWithoutBus_ImportsAsUnknown()
        {
            ParsedFeederList result = Parse(Header, [1, null, "c1"], [2, "A", "c2"]);
            Assert.Equal([new ConsumerEntry("A", "c2"), new ConsumerEntry(null, "c1")], result.Entries);
        }

        [Fact]
        public void Read_DuplicateConsumer_Fatal()
        {
            ParsedFeederList result = Parse(Header, [1, "A", "c0"], [2, "A", "c1"], [3, "B", "c2"], [4, "B", "c3"], [5, "B", "c4"], [6, "C", "c1"]);
            Assert.Equal("Consumer ID c1 appears more than once (rows 3, 7).", result.FatalError);
        }

        [Fact]
        public void Read_MissingColumn_Fatal()
        {
            ParsedFeederList result = Parse(["Count", "Consumer ID"], [1, "c1"]);
            Assert.Contains("Missing column(s): Bus ID", result.FatalError);
            Assert.Contains("Settings tab", result.FatalError);
        }

        [Fact]
        public void Read_Empty_Fatal()
        {
            ParsedFeederList result = FeederListParser.Read(TestFiles.WriteRawText("", ".csv"), FeederColumns.Defaults);
            Assert.Equal("The file is empty.", result.FatalError);
        }

        [Fact]
        public void Read_NoConsumers_Fatal()
        {
            ParsedFeederList result = Parse(Header);
            Assert.Equal("No consumers were found in the file.", result.FatalError);
        }

        [Fact]
        public void Read_LockedFile_FatalCloseInExcel()
        {
            string path = TestFiles.WriteXlsx([Header, [1, "A", "c1"]]);
            using var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            ParsedFeederList result = FeederListParser.Read(path, FeederColumns.Defaults);
            Assert.Contains("close it and try again", result.FatalError);
        }
    }
}
