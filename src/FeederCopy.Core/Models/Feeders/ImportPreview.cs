namespace FeederCopy.Core.Models.Feeders
{
    public sealed record ImportPreview(
        string Title,
        IReadOnlyList<string> Feeders,
        IReadOnlyList<string> Problems,
        IReadOnlyList<string> Notes,
        bool CanSave);
}
