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
        public void Toggle_TickAddsCodeRule_UntickDefaultAddsException()
        {
            Assert.Equal("sp, jnc, BFSEC:jn", BusTags.Toggle("sp, jnc", "BFSEC", "jn", true));
            Assert.Equal("sp, jnc, -BFSEC:jnc", BusTags.Toggle("sp, jnc", "BFSEC", "jnc", false));
        }

        [Fact]
        public void Toggle_BackToDefault_RemovesRule_KeepsOtherCodes()
        {
            Assert.Equal("sp, jnc, AFSEC:jn", BusTags.Toggle("sp, jnc, -BFSEC:jnc, AFSEC:jn", "BFSEC", "jnc", true));
            Assert.Equal("sp, jnc, -AFSEC:sp", BusTags.Toggle("sp, jnc, BFSEC:jn, -AFSEC:sp", "bfsec", "JN", false));
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
