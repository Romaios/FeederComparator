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
        private static readonly XName WorksheetName = XName.Get("Worksheet", Ns);
        private static readonly XName TableName = XName.Get("Table", Ns);
        private static readonly XName RowName = XName.Get("Row", Ns);
        private static readonly XName SheetNameName = XName.Get("Name", Ns);

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

        public IReadOnlyList<TableSheet> ReadSheets(string path)
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreWhitespace = true, IgnoreComments = true };
            using XmlReader reader = XmlReader.Create(path, settings);
            XDocument document = XDocument.Load(reader);
            var sheets = new List<TableSheet>();
            foreach (XElement worksheet in document.Descendants(WorksheetName))
            {
                var rows = new List<IReadOnlyList<string?>>();
                foreach (XElement row in worksheet.Elements(TableName).Elements(RowName))
                {
                    if (int.TryParse((string?)row.Attribute(IndexName), out int index))
                    {
                        while (rows.Count < index - 1)
                            rows.Add([]);
                    }
                    rows.Add(ToCells(row));
                }
                sheets.Add(new TableSheet((string?)worksheet.Attribute(SheetNameName) ?? $"Sheet{sheets.Count + 1}", rows));
            }
            return sheets;
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
