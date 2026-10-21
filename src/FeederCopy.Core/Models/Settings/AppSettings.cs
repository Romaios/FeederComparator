namespace FeederCopy.Core.Models.Settings
{
    public enum ExportStyle
    {
        OneWorkbook,
        SeparateFiles,
    }

    public sealed record FeederColumns
    {
        public required string BusId { get; init; }
        public required string ConsumerId { get; init; }
        public string Status { get; init; } = "Status";

        public static FeederColumns Defaults { get; } = new() { BusId = "From Bus ID", ConsumerId = "To Customer ID", Status = "Status" };
    }

    public sealed record AsBilledColumns
    {
        public required string AccountNumber { get; init; }
        public required string KwhUsed { get; init; }
        public required string BillMonth { get; init; }

        public static AsBilledColumns Defaults { get; } = new() { AccountNumber = "ACCOUNTNUMBER", KwhUsed = "KWHUSED", BillMonth = "BILLMONTH" };
    }

    public sealed record AppSettings
    {
        public required FeederColumns Feeder { get; init; }
        public required AsBilledColumns AsBilled { get; init; }
        public required ExportStyle Export { get; init; }
        public string HandPlacedBusTags { get; init; } = "sp, jnc";
        public ThemeMode Theme { get; init; } = ThemeMode.Light;

        public static AppSettings Defaults { get; } = new()
        {
            Feeder = FeederColumns.Defaults,
            AsBilled = AsBilledColumns.Defaults,
            Export = ExportStyle.OneWorkbook,
        };
    }

    public sealed record SettingsLoadResult(AppSettings Settings, string? Problem);
}
