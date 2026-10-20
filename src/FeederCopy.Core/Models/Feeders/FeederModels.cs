namespace FeederCopy.Core.Models.Feeders
{
    public sealed record ConsumerEntry(string? BusId, string ConsumerId, bool Active = true);

    public sealed record FeederSummary(long Id, string Name, int Consumers, int Buses);

    public sealed record ConsumerLocation(long FeederId, string FeederName, string? BusId);

    public sealed record ConsumerRow(int Count, string BusLabel, string ConsumerId, bool Active = true)
    {
        public string StatusLabel => StatusLabels.For(Active);
    }

    public static class BusLabels
    {
        public const string Unknown = "Unknown";

        public static string For(string? busId) => busId ?? Unknown;
    }

    public static class StatusLabels
    {
        public const string Active = "Active";
        public const string Inactive = "Inactive";
        public const string MissingReading = "Missing reading";
        public const string InactiveButBilled = "Inactive but billed";
        public const string DuplicateInAsBilled = "Duplicate in As-Billed";
        public const string Unreadable = "kWh unreadable";

        public static string For(bool active) => active ? Active : Inactive;
    }
}
