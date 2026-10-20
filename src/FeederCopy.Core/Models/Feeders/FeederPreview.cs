namespace FeederCopy.Core.Models.Feeders
{
    public enum PreviewMode
    {
        NewFeeder,
        Update,
    }

    public sealed record FeederPreview(PreviewMode Mode, string Name, string Summary, IReadOnlyList<string> Problems, bool CanSave);
}
