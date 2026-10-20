using System.Xml;

namespace FeederCopy.Core.Services.Import
{
    public static class ReadFailure
    {
        public static string? Describe(Exception ex) => ex switch
        {
            InvalidDataException => ex.Message,
            XmlException => $"The file could not be read as an Excel XML spreadsheet ({ex.Message}).",
            IOException => $"The file could not be opened. If it is open in Excel, close it and try again. ({ex.Message})",
            UnauthorizedAccessException => $"Access to the file was denied. ({ex.Message})",
            _ => null,
        };
    }
}
