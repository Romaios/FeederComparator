namespace FeederCopy.Core.Models.Settings
{
    public static class SettingsValidator
    {
        public static IReadOnlyList<string> Validate(AppSettings settings)
        {
            var errors = new List<string>();
            FeederColumns feeder = settings.Feeder;
            AsBilledColumns asBilled = settings.AsBilled;

            if (IsBlank(feeder.BusId))
                errors.Add("Enter the feeder list column name for Bus ID.");
            if (IsBlank(feeder.ConsumerId))
                errors.Add("Enter the feeder list column name for Consumer ID.");
            if (!IsBlank(feeder.BusId) && Same(feeder.BusId, feeder.ConsumerId))
                errors.Add("Bus ID and Consumer ID can't use the same column name.");
            if (!IsBlank(feeder.Status) && (Same(feeder.Status, feeder.BusId) || Same(feeder.Status, feeder.ConsumerId)))
                errors.Add("Status can't use the same column name as Bus ID or Consumer ID.");

            if (IsBlank(asBilled.AccountNumber))
                errors.Add("Enter the As-Billed column name for ACCOUNTNUMBER.");
            if (IsBlank(asBilled.KwhUsed))
                errors.Add("Enter the As-Billed column name for KWHUSED.");
            if (IsBlank(asBilled.BillMonth))
                errors.Add("Enter the As-Billed column name for BILLMONTH.");

            foreach (string tag in Feeders.BusSorter.ParseTags(settings.HandPlacedBusTags).Where(t => !t.All(char.IsLetter)))
                errors.Add($"Bus tag '{tag}' can only contain letters.");

            string[] names = [asBilled.AccountNumber, asBilled.KwhUsed, asBilled.BillMonth];
            List<string> filled = names.Where(n => !IsBlank(n)).Select(n => n.Trim()).ToList();
            if (filled.Distinct(StringComparer.OrdinalIgnoreCase).Count() != filled.Count)
                errors.Add("The As-Billed column names must all be different.");

            return errors;
        }

        private static bool IsBlank(string? text) => string.IsNullOrWhiteSpace(text);

        private static bool Same(string a, string b) =>
            string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
