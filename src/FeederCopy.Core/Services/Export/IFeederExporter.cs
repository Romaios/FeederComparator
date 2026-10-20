using FeederCopy.Core.Models.Copy;

namespace FeederCopy.Core.Services.Export
{
    public interface IFeederExporter
    {
        void WriteWorkbook(string path, CopyResult result);
        IReadOnlyList<string> SeparatePaths(string folder, CopyResult result);
        void WriteSeparate(string folder, CopyResult result);
    }
}
