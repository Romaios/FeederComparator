using System.Xml;
using System.Xml.Linq;

namespace FeederCopy.Core.Services.Import
{
    public sealed class SpreadsheetMlReader : ITableReader
    {
        private const string Ns = "urn:schemas-microsoft-com:office:spreadsheet";
        private static readonly XName CellName = XName.Get("Cell", Ns);
        private static readonly XName DataName = XName.Get("Data", Ns);
        private static readonly XName IndexName = XName.Get("Index", Ns);

        public IEnumerable<IReadOnlyList<string?>> ReadRows(string path)
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreWhitespace = true, IgnoreComments = true };
            using XmlReader reader = XmlReader.Create(path, settings);
            bool inWorksheet = false;
            reader.MoveToContent();
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == Ns && reader.LocalName == "Worksheet")
                {
                    inWorksheet = true;
                    reader.Read();
                }
                else if (inWorksheet && reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == Ns && reader.LocalName == "Row")
                {
                    var row = (XElement)XNode.ReadFrom(reader);
                    yield return ToCells(row);
                }
                else if (inWorksheet && reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "Worksheet")
                {
                    yield break;
                }
                else
                {
                    reader.Read();
                }
            }
        }

        private static List<string?> ToCells(XElement row)
        {
            var cells = new List<string?>();
            foreach (XElement cell in row.Elements(CellName))
            {
                if (int.TryParse((string?)cell.Attribute(IndexName), out int index))
                {
                    while (cells.Count < index - 1)
                        cells.Add(null);
                }
                cells.Add(cell.Element(DataName)?.Value);
            }
            return cells;
        }
    }
}
