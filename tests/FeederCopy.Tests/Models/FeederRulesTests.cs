using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Tests.Models
{
    public sealed class FeederRulesTests
    {
        private static readonly FeederSummary[] Existing = [new FeederSummary(1, "New York Feeder", 10, 2)];

        [Fact]
        public void ValidateName_Blank() =>
            Assert.Equal("Enter a feeder name.", FeederRules.ValidateName("  ", Existing, null));

        [Fact]
        public void ValidateName_Duplicate_CaseInsensitive() =>
            Assert.Equal("A feeder named 'new york feeder' already exists.", FeederRules.ValidateName("new york feeder", Existing, null));

        [Fact]
        public void ValidateName_SameIdAllowed() =>
            Assert.Null(FeederRules.ValidateName("New York Feeder", Existing, 1));

        [Fact]
        public void ValidateName_NewName_Ok() =>
            Assert.Null(FeederRules.ValidateName("Brooklyn Feeder", Existing, null));

        [Fact]
        public void ValidateConsumerId_Blank() =>
            Assert.Equal("Enter a consumer ID.", FeederRules.ValidateConsumerId(" "));

        [Fact]
        public void AlreadyIn_WithBus() =>
            Assert.Equal("Consumer c1 is already in New York Feeder under bus A.",
                FeederRules.AlreadyIn("c1", new ConsumerLocation(1, "New York Feeder", "A")));

        [Fact]
        public void AlreadyIn_Unknown() =>
            Assert.Equal("Consumer c1 is already in New York Feeder under bus Unknown.",
                FeederRules.AlreadyIn("c1", new ConsumerLocation(1, "New York Feeder", null)));
    }
}
