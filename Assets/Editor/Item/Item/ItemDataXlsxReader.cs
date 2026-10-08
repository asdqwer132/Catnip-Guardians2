#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ItemDataExcelTools
{
    // Reads .xlsx directly. No Excel installation or third-party DLL is required.
    internal static class ItemDataXlsxReader
    {
        internal sealed class Cell
        {
            public string Text = "";
            public bool HasFormula;
            public bool IsError;
        }

        internal sealed class Row
        {
            public int Number;
            public readonly Dictionary<int, Cell> Cells = new Dictionary<int, Cell>();
            public bool IsEmpty { get { return Cells.Values.All(c => !c.HasFormula && !c.IsError && string.IsNullOrWhiteSpace(c.Text)); } }
            public Cell At(int column)
            {
                Cell cell;
                return Cells.TryGetValue(column, out cell) ? cell : new Cell();
            }
        }

        internal sealed class Sheet
        {
            public string Name;
            public readonly List<Row> Rows = new List<Row>();
        }

        private const long MaxXmlSize = 64L * 1024L * 1024L;
        private const int MaxRows = 100000;

        public static Dictionary<string, Sheet> Read(string filePath)
        {
            var sheets = new Dictionary<string, Sheet>(StringComparer.OrdinalIgnoreCase);
            // FileShare.ReadWrite allows a saved workbook to be read while Excel is open.
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                XDocument workbook = LoadXml(zip, "xl/workbook.xml");
                XDocument relationships = LoadXml(zip, "xl/_rels/workbook.xml.rels");
                var targets = new Dictionary<string, string>(StringComparer.Ordinal);
                var sharedStrings = new List<string>();
                foreach (XElement relation in relationships.Descendants().Where(e => e.Name.LocalName == "Relationship"))
                {
                    if (Attr(relation, "TargetMode") == "External")
                        continue;
                    string id = Attr(relation, "Id");
                    string part = ResolvePart("xl", Attr(relation, "Target"));
                    targets.Add(id, part);
                    if (Attr(relation, "Type").EndsWith("/sharedStrings", StringComparison.Ordinal))
                        sharedStrings = ReadSharedStrings(LoadXml(zip, part));
                }
                // Some writers omit the sharedStrings relationship.
                if (sharedStrings.Count == 0 && zip.GetEntry("xl/sharedStrings.xml") != null)
                    sharedStrings = ReadSharedStrings(LoadXml(zip, "xl/sharedStrings.xml"));

                foreach (XElement element in workbook.Descendants().Where(e => e.Name.LocalName == "sheet"))
                {
                    string name = Attr(element, "name");
                    string id = Attr(element, "id");
                    string part;
                    if (!targets.TryGetValue(id, out part))
                        throw new InvalidDataException("시트 연결 정보를 찾을 수 없습니다: " + name);
                    if (!part.StartsWith("xl/worksheets/", StringComparison.Ordinal))
                        continue; // Chart sheets are not data worksheets.
                    var sheet = new Sheet { Name = name };
                    XDocument document = LoadXml(zip, part);
                    foreach (XElement rowElement in document.Descendants().Where(e => e.Name.LocalName == "row"))
                    {
                        int number;
                        if (!int.TryParse(Attr(rowElement, "r"), NumberStyles.None, CultureInfo.InvariantCulture, out number))
                            throw new InvalidDataException(name + ": 행 번호가 올바르지 않습니다.");
                        if (number < 1 || number > MaxRows)
                            throw new InvalidDataException(name + ": 지원하는 최대 행 수는 " + MaxRows + "입니다.");
                        var row = new Row { Number = number };
                        foreach (XElement cellElement in rowElement.Elements().Where(e => e.Name.LocalName == "c"))
                        {
                            string address = Attr(cellElement, "r");
                            int column = ColumnIndex(address);
                            if (row.Cells.ContainsKey(column))
                                throw new InvalidDataException(name + "!" + address + ": 중복 셀입니다.");
                            string type = Attr(cellElement, "t");
                            XElement valueElement = cellElement.Elements().FirstOrDefault(e => e.Name.LocalName == "v");
                            string value = valueElement == null ? "" : valueElement.Value;
                            var cell = new Cell
                            {
                                HasFormula = cellElement.Elements().Any(e => e.Name.LocalName == "f"),
                                IsError = type == "e"
                            };
                            if (type == "s")
                            {
                                int index;
                                if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out index) ||
                                    index < 0 || index >= sharedStrings.Count)
                                    throw new InvalidDataException(name + "!" + address + ": 문자열 인덱스가 올바르지 않습니다.");
                                cell.Text = DecodeExcelString(sharedStrings[index]);
                            }
                            else if (type == "inlineStr")
                            {
                                XElement inline = cellElement.Elements().FirstOrDefault(e => e.Name.LocalName == "is");
                                cell.Text = inline == null ? "" : DecodeExcelString(ReadText(inline));
                            }
                            else if (type == "b")
                            {
                                if (value != "0" && value != "1")
                                    throw new InvalidDataException(name + "!" + address + ": 논리값이 올바르지 않습니다.");
                                cell.Text = value == "1" ? "TRUE" : "FALSE";
                            }
                            else
                                cell.Text = type == "str" ? DecodeExcelString(value) : value;
                            row.Cells.Add(column, cell);
                        }
                        sheet.Rows.Add(row);
                    }
                    if (sheet.Rows.GroupBy(r => r.Number).Any(g => g.Count() != 1))
                        throw new InvalidDataException(name + ": 중복 행 번호가 있습니다.");
                    sheet.Rows.Sort((a, b) => a.Number.CompareTo(b.Number));
                    sheets.Add(name, sheet);
                }
            }
            return sheets;
        }

        private static XDocument LoadXml(ZipArchive zip, string part)
        {
            ZipArchiveEntry entry = zip.GetEntry(part);
            if (entry == null)
                throw new InvalidDataException("XLSX 구성 파일이 없습니다: " + part);
            if (entry.Length > MaxXmlSize)
                throw new InvalidDataException("시트 XML 크기가 너무 큽니다: " + part);
            using (Stream stream = entry.Open())
            using (XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaxXmlSize
            }))
                return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        }

        private static List<string> ReadSharedStrings(XDocument document)
        {
            return document.Root.Elements().Where(e => e.Name.LocalName == "si").Select(ReadText).ToList();
        }

        private static string ReadText(XElement parent)
        {
            // Ignore phonetic annotations (rPh); concatenate plain and rich text runs.
            var text = new StringBuilder();
            foreach (XElement child in parent.Elements())
            {
                if (child.Name.LocalName == "t") text.Append(child.Value);
                else if (child.Name.LocalName == "r")
                    foreach (XElement run in child.Elements().Where(e => e.Name.LocalName == "t"))
                        text.Append(run.Value);
            }
            return text.ToString();
        }

        private static string DecodeExcelString(string value)
        {
            // A single pass preserves escaped literal text such as _x005F_x0041_.
            return Regex.Replace(value, "_x([0-9a-fA-F]{4})_", m =>
                ((char)int.Parse(m.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToString());
        }

        private static string Attr(XElement element, string name)
        {
            XAttribute attribute = element.Attributes().FirstOrDefault(a => a.Name.LocalName == name);
            return attribute == null ? "" : attribute.Value;
        }

        private static int ColumnIndex(string address)
        {
            int result = 0;
            int letters = 0;
            foreach (char character in address)
            {
                if (character >= 'A' && character <= 'Z')
                {
                    result = checked(result * 26 + character - 'A' + 1);
                    letters++;
                }
                else break;
            }
            if (letters == 0 || result > 16384)
                throw new InvalidDataException("셀 주소가 올바르지 않습니다: " + address);
            return result - 1;
        }

        private static string ResolvePart(string directory, string target)
        {
            if (string.IsNullOrEmpty(target)) throw new InvalidDataException("빈 XLSX 연결 경로입니다.");
            string combined = target.StartsWith("/", StringComparison.Ordinal)
                ? target.Substring(1) : directory + "/" + target;
            var parts = new List<string>();
            foreach (string part in Uri.UnescapeDataString(combined).Replace('\\', '/').Split('/'))
            {
                if (part.Length == 0 || part == ".") continue;
                if (part == "..")
                {
                    if (parts.Count == 0) throw new InvalidDataException("잘못된 XLSX 연결 경로입니다.");
                    parts.RemoveAt(parts.Count - 1);
                }
                else parts.Add(part);
            }
            return string.Join("/", parts.ToArray());
        }
    }
}
#endif
