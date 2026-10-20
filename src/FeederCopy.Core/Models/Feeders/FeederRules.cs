namespace FeederCopy.Core.Models.Feeders
{
    public static class FeederRules
    {
        public static string? ValidateName(string? name, IEnumerable<FeederSummary> existing, long? exceptId)
        {
            string trimmed = name?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
                return "Enter a feeder name.";
            bool taken = existing.Any(f => f.Id != exceptId && string.Equals(f.Name, trimmed, StringComparison.OrdinalIgnoreCase));
            return taken ? $"A feeder named '{trimmed}' already exists." : null;
        }

        public static string? ValidateConsumerId(string? id) =>
            string.IsNullOrWhiteSpace(id) ? "Enter a consumer ID." : null;

        public static string AlreadyIn(string consumerId, ConsumerLocation where) =>
            $"Consumer {consumerId} is already in {where.FeederName} under bus {BusLabels.For(where.BusId)}.";
    }
}
