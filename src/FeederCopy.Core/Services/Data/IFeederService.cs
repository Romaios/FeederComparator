using FeederCopy.Core.Models.Feeders;

namespace FeederCopy.Core.Services.Data
{
    public interface IFeederService
    {
        IReadOnlyList<FeederSummary> GetFeeders();
        IReadOnlyList<ConsumerEntry> GetConsumers(long feederId);
        IReadOnlyList<(string FeederName, ConsumerEntry Entry)> GetAllConsumers();
        void SetActive(string consumerId, bool active);
        ConsumerLocation? FindConsumer(string consumerId);
        IReadOnlyList<(string ConsumerId, ConsumerLocation Where)> FindConsumers(IEnumerable<string> consumerIds);
        long CreateFeeder(string name, IReadOnlyList<ConsumerEntry> consumers);
        void SaveConsumers(IReadOnlyDictionary<long, IReadOnlyList<ConsumerEntry>> byFeeder);
        IReadOnlyList<long> ImportFeeders(IReadOnlyList<(string Name, IReadOnlyList<ConsumerEntry> Consumers)> feeders);
        void RenameFeeder(long feederId, string name);
        void DeleteFeeder(long feederId);
    }
}
