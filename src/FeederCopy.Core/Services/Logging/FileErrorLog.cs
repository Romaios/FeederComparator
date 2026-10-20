using System.Diagnostics;

namespace FeederCopy.Core.Services.Logging
{
    public sealed class FileErrorLog : IErrorLog
    {
        private readonly string _folder;

        public FileErrorLog(string folder) => _folder = folder;

        public void Error(string context, Exception exception)
        {
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}";
            try
            {
                Directory.CreateDirectory(_folder);
                File.AppendAllText(Path.Combine(_folder, $"{DateTime.Now:yyyy-MM-dd}.log"), entry);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Trace.WriteLine($"Could not write to the log folder '{_folder}': {ex.Message}{Environment.NewLine}{entry}");
            }
        }
    }
}
