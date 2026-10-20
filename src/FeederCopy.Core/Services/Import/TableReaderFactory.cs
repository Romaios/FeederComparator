using System.Text;

namespace FeederCopy.Core.Services.Import
{
    public static class TableReaderFactory
    {
        private static readonly byte[] BinaryExcelSignature = [0xD0, 0xCF, 0x11, 0xE0];

        public static ITableReader For(string path)
        {
            var head = new byte[16];
            int read;
            using (FileStream stream = File.OpenRead(path))
                read = stream.Read(head, 0, head.Length);
            ReadOnlySpan<byte> bytes = head.AsSpan(0, read);

            if (bytes.StartsWith("PK"u8))
                return new XlsxReader();
            if (bytes.StartsWith(BinaryExcelSignature))
                throw new InvalidDataException("This is an old binary Excel (.xls) file. Open it in Excel and use Save As → Excel Workbook (.xlsx), then use that file.");

            string text = Encoding.UTF8.GetString(bytes).TrimStart('﻿', ' ', '\t', '\r', '\n');
            if (text.StartsWith('<'))
                return new SpreadsheetMlReader();
            if (Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                return new CsvFileReader();

            throw new InvalidDataException("Unsupported file format. Expected an .xls XML spreadsheet, an .xlsx workbook, or a .csv file.");
        }
    }
}
