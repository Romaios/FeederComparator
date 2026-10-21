using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Services.Settings;

namespace FeederCopy.Tests.Services
{
    public sealed class SettingsStoreTests
    {
        [Fact]
        public void Load_MissingFile_DefaultsNoProblem()
        {
            SettingsLoadResult result = new SettingsStore(TestFiles.TempPath(".json")).Load();
            Assert.Equal(AppSettings.Defaults, result.Settings);
            Assert.Null(result.Problem);
        }

        [Fact]
        public void Load_BadJson_DefaultsWithProblem()
        {
            string path = TestFiles.WriteRawText("{ not json", ".json");
            SettingsLoadResult result = new SettingsStore(path).Load();
            Assert.Equal(AppSettings.Defaults, result.Settings);
            Assert.StartsWith("settings.json could not be used (", result.Problem);
        }

        [Fact]
        public void Load_MissingRequired_DefaultsWithProblem()
        {
            string path = TestFiles.WriteRawText("{ \"Export\": \"OneWorkbook\" }", ".json");
            SettingsLoadResult result = new SettingsStore(path).Load();
            Assert.Equal(AppSettings.Defaults, result.Settings);
            Assert.NotNull(result.Problem);
        }

        [Fact]
        public void Load_InvalidValues_DefaultsWithProblem()
        {
            string path = TestFiles.TempPath(".json");
            var store = new SettingsStore(path);
            store.Save(AppSettings.Defaults with { Feeder = FeederColumns.Defaults with { BusId = "" } });
            SettingsLoadResult result = store.Load();
            Assert.Equal(AppSettings.Defaults, result.Settings);
            Assert.Contains("Bus ID", result.Problem);
        }

        [Fact]
        public void Load_OldFileWithoutStatus_UsesDefaultStatusName()
        {
            string json = """
                {
                  "Feeder": { "BusId": "From Bus ID", "ConsumerId": "To Customer ID" },
                  "AsBilled": { "AccountNumber": "ACCOUNTNUMBER", "KwhUsed": "KWHUSED", "BillMonth": "BILLMONTH" },
                  "Export": "OneWorkbook"
                }
                """;
            SettingsLoadResult result = new SettingsStore(TestFiles.WriteRawText(json, ".json")).Load();
            Assert.Null(result.Problem);
            Assert.Equal("Status", result.Settings.Feeder.Status);
            Assert.Equal("sp, jnc", result.Settings.HandPlacedBusTags);
            Assert.Equal(ThemeMode.Light, result.Settings.Theme);
        }

        [Fact]
        public void SaveThenLoad_RoundTrips()
        {
            string path = TestFiles.TempPath(".json");
            var store = new SettingsStore(path);
            AppSettings saved = AppSettings.Defaults with
            {
                Export = ExportStyle.SeparateFiles,
                Feeder = new FeederColumns { BusId = "BUS", ConsumerId = "ACCT" },
            };
            store.Save(saved);
            SettingsLoadResult result = store.Load();
            Assert.Equal(saved, result.Settings);
            Assert.Null(result.Problem);
            Assert.Contains("\"SeparateFiles\"", File.ReadAllText(path));
        }
    }
}
