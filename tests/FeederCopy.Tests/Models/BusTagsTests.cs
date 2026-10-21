using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Tests.Models
{
    public sealed class BusTagsTests
    {
        private static readonly (string Feeder, string Bus)[] Buses =
        [
            ("Abuyog", "AFSEC3-5jnc1"),
            ("Abuyog", "AFSEC3-6Ajnc1"),
            ("Abuyog", "AFSEC3-6Qsp1"),
            ("Abuyog", "AFSEC3-6D"),
            ("Bacon", "BFSEC1Pjn2"),
            ("Commercial", "CFSEC26-4ASP1"),
            ("Commercial", "CFSEC1-1SP2"),
        ];

        [Theory]
        [InlineData("AFSEC3-5jnc1", "AFSEC")]
        [InlineData("CF-PSEC13jnc1", "CF-PSEC")]
        [InlineData("PF-SEC1-1jnc1", "PF-SEC")]
        [InlineData("NODIGITS", "NODIGITS")]
        public void Code_IsTextBeforeFirstNumber(string bus, string code) =>
            Assert.Equal(code, BusTags.Code(bus));

        [Fact]
        public void Discover_TagsPerFeederAndCode()
        {
            IReadOnlyList<BusTagRow> rows = BusTags.Discover(Buses, BusTags.ParseRules("sp, jnc"));
            Assert.Equal(
            [
                new BusTagRow("Abuyog", "AFSEC", "jnc", 2, "AFSEC3-5jnc1", true),
                new BusTagRow("Abuyog", "AFSEC", "sp", 1, "AFSEC3-6Qsp1", true),
                new BusTagRow("Bacon", "BFSEC", "jn", 1, "BFSEC1Pjn2", false),
                new BusTagRow("Commercial", "CFSEC", "SP", 2, "CFSEC26-4ASP1", true),
            ], rows);
        }

        [Fact]
        public void Discover_UsesPerCodeRules()
        {
            IReadOnlyList<BusTagRow> rows = BusTags.Discover(Buses, BusTags.ParseRules("AFSEC:jnc, CFSEC:sp"));
            Assert.Equal([true, false, false, true], rows.Select(r => r.HandPlaced));
        }

        [Fact]
        public void ToSetting_CheckedRowsAsCodeAndTag()
        {
            BusTagRow[] rows =
            [
                new("Gubat", "GPSEC", "sp", 3, "GPSEC5sp1", true),
                new("Payawin", "GPSEC", "sp", 2, "GPSEC9sp1", true),
                new("Abuyog", "AFSEC", "jnc", 2, "AFSEC3-5jnc1", true),
                new("Bacon", "BFSEC", "jn", 1, "BFSEC1Pjn2", false),
            ];
            Assert.Equal("GPSEC:sp, AFSEC:jnc", BusTags.ToSetting(rows, []));
        }

        [Fact]
        public void ToSetting_KeepsDefaultsAndWritesOnlyExceptions()
        {
            BusTagRow[] rows =
            [
                new("Abuyog", "AFSEC", "jnc", 2, "AFSEC3-5jnc1", true),
                new("Bacon", "BFSEC", "jnc", 9, "BFSEC1jnc1", false),
                new("Bacon", "BFSEC", "jn", 1, "BFSEC1Pjn2", true),
            ];
            Assert.Equal("sp, jnc, -BFSEC:jnc, BFSEC:jn", BusTags.ToSetting(rows, BusTags.ParseRules("sp, jnc")));
        }

        [Fact]
        public void Matches_ExceptionBeatsDefault()
        {
            IReadOnlyList<BusTagRule> rules = BusTags.ParseRules("sp, jnc, -BFSEC:jnc, BFSEC:jn");
            Assert.False(BusTags.Matches("BFSEC1jnc1", rules));
            Assert.True(BusTags.Matches("BFSEC1Pjn2", rules));
            Assert.True(BusTags.Matches("AFSEC1jnc1", rules));
            Assert.True(BusTags.Matches("BFSEC1sp1", rules));
        }

        [Fact]
        public void Discover_ExceptionUntick()
        {
            IReadOnlyList<BusTagRow> rows = BusTags.Discover(Buses, BusTags.ParseRules("sp, jnc, -AFSEC:jnc"));
            Assert.False(rows.Single(r => r.Code == "AFSEC" && r.Tag == "jnc").HandPlaced);
        }

        [Fact]
        public void Matches_GlobalOrSameCodeOnly()
        {
            Assert.True(BusTags.Matches("AFSEC3-5jnc1", BusTags.ParseRules("jnc")));
            Assert.True(BusTags.Matches("AFSEC3-5jnc1", BusTags.ParseRules("afsec:JNC")));
            Assert.False(BusTags.Matches("AFSEC3-5jnc1", BusTags.ParseRules("BFSEC:jnc")));
            Assert.True(BusTags.Matches("CFSEC26-4ASP1", BusTags.ParseRules("CFSEC:sp")));
            Assert.False(BusTags.Matches("AFSEC3-6D", BusTags.ParseRules("jnc, sp")));
        }

        [Fact]
        public void MatchedTag_ReturnsTheTagText() =>
            Assert.Equal("jnc", BusTags.MatchedTag("AFSEC3-5jnc1", BusTags.ParseRules("sp, jnc")));
    }
}
