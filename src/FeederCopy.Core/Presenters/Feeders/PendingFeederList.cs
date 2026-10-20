using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Import;

namespace FeederCopy.Core.Presenters.Feeders
{
    public sealed record PendingFeederList(
        PreviewMode Mode,
        long? FeederId,
        ParsedFeederList Parsed,
        IReadOnlyList<string> Conflicts,
        string Summary,
        string Name)
    {
        public FeederPreview ToPreview(IEnumerable<FeederSummary> existing)
        {
            var problems = new List<string>();
            if (Parsed.FatalError is not null)
                problems.Add(Parsed.FatalError);
            problems.AddRange(Parsed.Warnings.Select(w => $"Row {w.RowNumber}: {w.Reason}"));
            problems.AddRange(Conflicts);
            string? nameError = FeederRules.ValidateName(Name, existing, FeederId);
            if (nameError is not null)
                problems.Add(nameError);

            bool canSave = Parsed.FatalError is null && Conflicts.Count == 0 && nameError is null;
            return new FeederPreview(Mode, Name, Summary, problems, canSave);
        }
    }
}

