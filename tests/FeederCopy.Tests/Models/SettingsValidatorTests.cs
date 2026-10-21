using FeederCopy.Core.Models.Settings;

namespace FeederCopy.Tests.Models
{
    public sealed class SettingsValidatorTests
    {
        [Fact]
        public void Validate_Defaults_NoErrors() =>
            Assert.Empty(SettingsValidator.Validate(AppSettings.Defaults));

        [Fact]
        public void Validate_BlankBusId_Error()
        {
            AppSettings s = AppSettings.Defaults with { Feeder = FeederColumns.Defaults with { BusId = " " } };
            Assert.Contains("Enter the feeder list column name for Bus ID.", SettingsValidator.Validate(s));
        }

        [Fact]
        public void Validate_BlankConsumerId_Error()
        {
            AppSettings s = AppSettings.Defaults with { Feeder = FeederColumns.Defaults with { ConsumerId = "" } };
            Assert.Contains("Enter the feeder list column name for Consumer ID.", SettingsValidator.Validate(s));
        }

        [Fact]
        public void Validate_BlankAsBilledNames_Errors()
        {
            AppSettings s = AppSettings.Defaults with { AsBilled = new AsBilledColumns { AccountNumber = "", KwhUsed = " ", BillMonth = "" } };
            IReadOnlyList<string> errors = SettingsValidator.Validate(s);
            Assert.Contains("Enter the As-Billed column name for ACCOUNTNUMBER.", errors);
            Assert.Contains("Enter the As-Billed column name for KWHUSED.", errors);
            Assert.Contains("Enter the As-Billed column name for BILLMONTH.", errors);
        }

        [Fact]
        public void Validate_SameFeederNames_Error()
        {
            AppSettings s = AppSettings.Defaults with { Feeder = new FeederColumns { BusId = "ID", ConsumerId = "id" } };
            Assert.Contains("Bus ID and Consumer ID can't use the same column name.", SettingsValidator.Validate(s));
        }

        [Fact]
        public void Validate_StatusSameAsBusId_Error()
        {
            AppSettings s = AppSettings.Defaults with { Feeder = FeederColumns.Defaults with { Status = "from bus id" } };
            Assert.Contains("Status can't use the same column name as Bus ID or Consumer ID.", SettingsValidator.Validate(s));
        }

        [Fact]
        public void Validate_BlankStatus_Allowed()
        {
            AppSettings s = AppSettings.Defaults with { Feeder = FeederColumns.Defaults with { Status = "" } };
            Assert.Empty(SettingsValidator.Validate(s));
        }

        [Fact]
        public void Validate_BusTagWithNonLetters_Error()
        {
            AppSettings s = AppSettings.Defaults with { HandPlacedBusTags = "sp, jnc-1" };
            Assert.Contains("Bus tag 'jnc-1' can only contain letters.", SettingsValidator.Validate(s));
        }

        [Fact]
        public void Validate_BusTagsLetters_Ok()
        {
            AppSettings s = AppSettings.Defaults with { HandPlacedBusTags = "sp, jnc, tap" };
            Assert.Empty(SettingsValidator.Validate(s));
        }

        [Fact]
        public void Validate_DuplicateAsBilledNames_Error()
        {
            AppSettings s = AppSettings.Defaults with { AsBilled = AsBilledColumns.Defaults with { KwhUsed = "accountnumber" } };
            Assert.Contains("The As-Billed column names must all be different.", SettingsValidator.Validate(s));
        }
    }
}
