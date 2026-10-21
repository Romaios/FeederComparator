using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Import;
using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Services.Import;

namespace FeederCopy.Tests.Services
{
    public sealed class AsBilledReaderTests
    {
        private static readonly string?[] Header = ["ADDRESS", "ACCOUNTNUMBER", "ACCOUNTNAME", "RATECODE", "KWHUSED", "BILLMONTH"];

        private static string?[] Row(string? account, string? kwh, string month = "202607") =>
            ["Somewhere", account, "Test Name", "R", kwh, month];

        private static KwhSnapshot Read(params string?[][] rows) =>
            new AsBilledReader().Read(TestFiles.WriteSpreadsheetMl([Header, .. rows]), AsBilledColumns.Defaults, null);

        [Fact]
        public void Read_IndexesByTrimmedAccount()
        {
            KwhSnapshot s = Read(Row(" 0101-0001 ", "150"));
            Assert.Null(s.FatalError);
            Assert.Equal([new AsBilledEntry(2, 150)], s.ByAccount["0101-0001"]);
            Assert.Equal(new BillMonth(2026, 7), s.Month);
            Assert.Equal(1, s.RowsRead);
        }

        [Fact]
        public void Read_2025Layout_KilowatthourAndMmyyMonth()
        {
            string?[] header2025 = ["ADDRESS", "ACCOUNTNUMBER", "NAME", "RATECODE", "KILOWATTHOUR", "BILLMONTH"];
            string path = TestFiles.WriteSpreadsheetMl([header2025, ["Somewhere", "0900001", "Test Name", "R", "77", "0825"]]);
            KwhSnapshot s = new AsBilledReader().Read(path, AsBilledColumns.Defaults, null);
            Assert.Null(s.FatalError);
            Assert.Equal([new AsBilledEntry(2, 77)], s.ByAccount["0900001"]);
            Assert.Equal(new BillMonth(2025, 8), s.Month);
        }

        [Fact]
        public void Read_NoListedName_ErrorNamesEveryChoice()
        {
            string path = TestFiles.WriteSpreadsheetMl([["ACCOUNTNUMBER", "KWH", "BILLMONTH"], ["c1", "1", "202607"]]);
            KwhSnapshot s = new AsBilledReader().Read(path, AsBilledColumns.Defaults, null);
            Assert.Contains("Missing column(s): KWHUSED or KILOWATTHOUR.", s.FatalError);
        }

        [Fact]
        public void Read_DuplicateAccount_KeepsBothEntries()
        {
            KwhSnapshot s = Read(Row("c1", "10"), Row("c2", "5"), Row("c1", "20"));
            Assert.Equal([new AsBilledEntry(2, 10), new AsBilledEntry(4, 20)], s.ByAccount["c1"]);
        }

        [Fact]
        public void Read_UnreadableKwh_EntryWithNullKwh()
        {
            KwhSnapshot s = Read(Row("c1", "abc"));
            Assert.Equal([new AsBilledEntry(2, null)], s.ByAccount["c1"]);
        }

        [Fact]
        public void Read_CommasInKwh_Parsed()
        {
            KwhSnapshot s = Read(Row("c1", "1,234.5"));
            Assert.Equal(1234.5, s.ByAccount["c1"][0].Kwh);
        }

        [Fact]
        public void Read_MissingAccount_RowError()
        {
            KwhSnapshot s = Read(Row("c1", "1"), Row(null, "5"));
            Assert.Equal([new RowError(3, "Missing account number")], s.Errors);
        }

        [Fact]
        public void Read_InvalidMonth_RowError()
        {
            KwhSnapshot s = Read(Row("c1", "1"), Row("c2", "5", "abc"));
            Assert.Equal([new RowError(3, "Invalid bill month 'abc' (expected YYYYMM, MMYYYY or MMYY)")], s.Errors);
        }

        [Fact]
        public void Read_TwoMonths_Fatal()
        {
            KwhSnapshot s = Read(Row("c1", "1", "202606"), Row("c2", "5", "202607"));
            Assert.Contains("more than one bill month", s.FatalError);
        }

        [Theory]
        [InlineData("202607")]
        [InlineData("072026")]
        [InlineData("0726")]
        public void Read_MonthFormats(string text)
        {
            KwhSnapshot s = Read(Row("c1", "1", text));
            Assert.Equal(new BillMonth(2026, 7), s.Month);
        }

        [Fact]
        public void Read_MissingColumn_Fatal()
        {
            string path = TestFiles.WriteSpreadsheetMl([["ACCOUNTNUMBER", "BILLMONTH"], ["c1", "202607"]]);
            KwhSnapshot s = new AsBilledReader().Read(path, AsBilledColumns.Defaults, null);
            Assert.Contains("Missing column(s): KWHUSED", s.FatalError);
        }

        [Fact]
        public void Read_NoValidRows_Fatal()
        {
            KwhSnapshot s = Read(Row(null, "1"));
            Assert.Equal("No valid rows were found in the file.", s.FatalError);
        }

        [Fact]
        public void Read_LockedFile_FatalCloseInExcel()
        {
            string path = TestFiles.WriteSpreadsheetMl([Header, Row("c1", "1")]);
            using var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            KwhSnapshot s = new AsBilledReader().Read(path, AsBilledColumns.Defaults, null);
            Assert.Contains("close it and try again", s.FatalError);
        }

        [Fact]
        public void Read_ReportsProgressEvery5000Rows()
        {
            var rows = Enumerable.Range(1, 10_500).Select(i => Row($"c{i}", "1")).ToArray();
            var reported = new List<int>();
            string path = TestFiles.WriteSpreadsheetMl([Header, .. rows]);
            new AsBilledReader().Read(path, AsBilledColumns.Defaults, new SyncProgress(reported.Add));
            Assert.Equal([5000, 10000], reported);
        }

        [Fact]
        public void Read_FileNameIsJustTheName()
        {
            KwhSnapshot s = Read(Row("c1", "1"));
            Assert.DoesNotContain(Path.DirectorySeparatorChar, s.FileName);
        }

        private sealed class SyncProgress(Action<int> report) : IProgress<int>
        {
            public void Report(int value) => report(value);
        }
    }
}
