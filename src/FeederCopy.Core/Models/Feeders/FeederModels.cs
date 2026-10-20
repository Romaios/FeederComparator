namespace FeederCopy.Core.Models.Feeders
{
    public sealed record ConsumerEntry(string? BusId, string ConsumerId);

    public sealed record FeederSummary(long Id, string Name, int Consumers, int Buses);

    public sealed record ConsumerLocation(long FeederId, string FeederName, string? BusId);

    public sealed record ConsumerRow(int Count, string BusLabel, string ConsumerId);

    public static class BusLabels
    {
        public const string Unknown = "Unknown";

        public static string For(string? busId) => busId ?? Unknown;
    }
}
