using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BlockRift.Core
{
    /// <summary>Bölüm verisinin BoardValidator kurallarını ihlal etmesi durumunda fırlatılır.</summary>
    public sealed class LevelValidationException : InvalidOperationException
    {
        public BoardValidationResult ValidationResult
        {
            get;
        }

        public LevelValidationException(string message, BoardValidationResult validationResult)
            : base(message)
        {
            ValidationResult = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
        }
    }

    /// <summary>
    /// Unity bağımsız, pure C# JSON dönüştürücüsüdür.
    /// Bölüm şemasını serileştirir, ayrıştırır ve yükleme sonrasında BoardValidator ile yapısal doğrulamayı çalıştırır.
    /// </summary>
    public static class LevelJsonSerializer
    {
        /// <summary>Domain dışı adaptörlerin aynı JSON ayrıştırıcısını ek alanlar için kullanmasını sağlar.</summary>
        public static Dictionary<string, object> ParseJsonObject(string json)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON içeriği boş olamaz.", nameof(json));
            return JsonParser.Parse(json) as Dictionary<string, object> ?? throw new FormatException("Kök JSON bir nesne olmalı.");
        }

        /// <summary>Adaptörlerin ek verilerini kaybetmeden JSON nesnesini yazar; oyun kuralı veya görsel tercih içermez.</summary>
        public static string WriteJsonObject(Dictionary<string, object> value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return WriteJsonValue(value);
        }

        private static string WriteJsonValue(object value)
        {
            if (value == null)
                return "null";
            if (value is string text)
                return EscapeString(text);
            if (value is bool flag)
                return flag ? "true" : "false";
            if (value is Dictionary<string, object> obj)
                return "{" + string.Join(",", obj.Select(p => EscapeString(p.Key) + ":" + WriteJsonValue(p.Value))) + "}";
            if (value is List<object> list)
                return "[" + string.Join(",", list.Select(WriteJsonValue)) + "]";
            if (value is long || value is int || value is double)
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            throw new FormatException("Desteklenmeyen JSON değeri: " + value.GetType().Name);
        }

        /// <summary>Doğrulanmış LevelDefinition nesnesini standart JSON metnine dönüştürür.</summary>
        public static string Serialize(LevelDefinition level, bool pretty = true)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));

            var sb = new StringBuilder();
            string indent = pretty ? "  " : "";
            string nl = pretty ? "\n" : "";
            string sp = pretty ? " " : "";

            sb.Append("{").Append(nl);
            sb.Append(indent).Append("\"id\":").Append(sp).Append(EscapeString(level.Id)).Append(",").Append(nl);
            sb.Append(indent).Append("\"schemaVersion\":").Append(sp).Append(level.SchemaVersion).Append(",").Append(nl);
            sb.Append(indent).Append("\"rulesVersion\":").Append(sp).Append(level.RulesVersion).Append(",").Append(nl);
            sb.Append(indent).Append("\"width\":").Append(sp).Append(level.Width).Append(",").Append(nl);
            sb.Append(indent).Append("\"height\":").Append(sp).Append(level.Height).Append(",").Append(nl);

            sb.Append(indent).Append("\"exit\":").Append(sp).Append("{").Append(sp);
            sb.Append("\"x\":").Append(sp).Append(level.Exit.X).Append(",").Append(sp);
            sb.Append("\"y\":").Append(sp).Append(level.Exit.Y).Append(sp);
            sb.Append("},").Append(nl);

            // Pieces
            sb.Append(indent).Append("\"pieces\":").Append(sp).Append("[").Append(nl);
            for (int i = 0; i < level.Pieces.Count; i++)
            {
                var p = level.Pieces[i];
                sb.Append(indent).Append(indent).Append("{").Append(sp);
                sb.Append("\"id\":").Append(sp).Append(EscapeString(p.Id)).Append(",").Append(sp);
                sb.Append("\"kind\":").Append(sp).Append(EscapeString(p.Kind.ToString())).Append(",").Append(sp);
                sb.Append("\"width\":").Append(sp).Append(p.Width).Append(",").Append(sp);
                sb.Append("\"height\":").Append(sp).Append(p.Height).Append(",").Append(sp);
                sb.Append("\"start\":").Append(sp).Append("{\"x\":").Append(sp).Append(p.Start.X).Append(",").Append(sp).Append("\"y\":").Append(sp).Append(p.Start.Y).Append("}");
                sb.Append(sp).Append("}");
                if (i < level.Pieces.Count - 1)
                    sb.Append(",");
                sb.Append(nl);
            }
            sb.Append(indent).Append("],").Append(nl);

            // FixedCells
            sb.Append(indent).Append("\"fixedCells\":").Append(sp).Append("[");
            if (level.FixedCells.Count > 0)
            {
                sb.Append(nl);
                for (int i = 0; i < level.FixedCells.Count; i++)
                {
                    var c = level.FixedCells[i];
                    sb.Append(indent).Append(indent).Append("{\"x\":").Append(sp).Append(c.X).Append(",").Append(sp).Append("\"y\":").Append(sp).Append(c.Y).Append("}");
                    if (i < level.FixedCells.Count - 1)
                        sb.Append(",");
                    sb.Append(nl);
                }
                sb.Append(indent);
            }
            sb.Append("],").Append(nl);

            // Triggers
            sb.Append(indent).Append("\"triggers\":").Append(sp).Append("[");
            if (level.Triggers.Count > 0)
            {
                sb.Append(nl);
                for (int i = 0; i < level.Triggers.Count; i++)
                {
                    var t = level.Triggers[i];
                    sb.Append(indent).Append(indent).Append("{\"x\":").Append(sp).Append(t.X).Append(",").Append(sp).Append("\"y\":").Append(sp).Append(t.Y).Append("}");
                    if (i < level.Triggers.Count - 1)
                        sb.Append(",");
                    sb.Append(nl);
                }
                sb.Append(indent);
            }
            sb.Append("]").Append(nl);

            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>JSON metnini ayrıştırır, şema ve kuralları doğrular, LevelDefinition döndürür.</summary>
        public static LevelDefinition Deserialize(string json)
        {
            var root = ParseJsonObject(json);

            string id = RequireString(root, "id");
            int schemaVersion = RequireInt(root, "schemaVersion");
            if (schemaVersion != 1)
                throw new FormatException("Desteklenmeyen schemaVersion: " + schemaVersion + ". Desteklenen sürüm: 1.");
            int rulesVersion = RequireInt(root, "rulesVersion");
            int width = RequireInt(root, "width");
            int height = RequireInt(root, "height");

            var exitObj = RequireObject(root, "exit");
            Cell exit = ParseCell(exitObj, "exit");

            var piecesList = RequireList(root, "pieces");
            var pieces = new List<PieceDefinition>();
            foreach (var item in piecesList)
            {
                if (!(item is Dictionary<string, object> pDict))
                    throw new FormatException("Parça tanımı bir nesne olmalıdır.");

                string pId = RequireString(pDict, "id");
                string kindStr = RequireString(pDict, "kind");
                if (!Enum.TryParse<PieceKind>(kindStr, true, out var kind))
                    throw new FormatException($"Bilinmeyen veya geçersiz parça türü: '{kindStr}'. İzin verilenler: Target, Obstacle.");

                int pWidth = RequireInt(pDict, "width");
                int pHeight = RequireInt(pDict, "height");
                var startObj = RequireObject(pDict, "start");
                Cell start = ParseCell(startObj, $"pieces[{pId}].start");

                pieces.Add(new PieceDefinition(pId, kind, pWidth, pHeight, start));
            }

            var fixedCells = new List<Cell>();
            if (root.TryGetValue("fixedCells", out var fcObj) && fcObj is List<object> fcList)
            {
                for (int i = 0; i < fcList.Count; i++)
                {
                    if (fcList[i] is Dictionary<string, object> cDict)
                        fixedCells.Add(ParseCell(cDict, $"fixedCells[{i}]"));
                    else
                        throw new FormatException($"fixedCells[{i}] geçerli bir hücre nesnesi değil.");
                }
            }

            var triggers = new List<Cell>();
            if (root.TryGetValue("triggers", out var trObj) && trObj is List<object> trList)
            {
                for (int i = 0; i < trList.Count; i++)
                {
                    if (trList[i] is Dictionary<string, object> tDict)
                        triggers.Add(ParseCell(tDict, $"triggers[{i}]"));
                    else
                        throw new FormatException($"triggers[{i}] geçerli bir hücre nesnesi değil.");
                }
            }

            var level = new LevelDefinition(id, schemaVersion, rulesVersion, width, height, exit, pieces, fixedCells, triggers);

            // GDD ve Core invariant kontrolü
            var validation = BoardValidator.Validate(level);
            if (!validation.IsValid)
            {
                string errors = string.Join(", ", validation.Errors);
                throw new LevelValidationException($"Bölüm yapısal olarak geçersiz ({id}): {errors}", validation);
            }

            return level;
        }

        private static Cell ParseCell(Dictionary<string, object> dict, string context)
        {
            int x = RequireInt(dict, "x", context);
            int y = RequireInt(dict, "y", context);
            return new Cell(x, y);
        }

        private static string RequireString(Dictionary<string, object> dict, string key, string context = null)
        {
            if (!dict.TryGetValue(key, out var val) || !(val is string str))
                throw new FormatException($"Eksik veya geçersiz alan '{key}'{(context != null ? " (" + context + ")" : "")}.");
            return str;
        }

        private static int RequireInt(Dictionary<string, object> dict, string key, string context = null)
        {
            if (!dict.TryGetValue(key, out var val))
                throw new FormatException($"Eksik alan '{key}'{(context != null ? " (" + context + ")" : "")}.");

            // Şema, boyut ve koordinatların hiçbirinde daraltma veya yuvarlama yapılmaz.
            if (val is long l && l >= int.MinValue && l <= int.MaxValue)
                return (int)l;
            if (val is int i)
                return i;
            if (val is double d && d >= int.MinValue && d <= int.MaxValue && d == Math.Truncate(d))
                return (int)d;

            throw new FormatException($"'{key}' bir tamsayı olmalıdır{(context != null ? " (" + context + ")" : "")}.");
        }

        private static Dictionary<string, object> RequireObject(Dictionary<string, object> dict, string key, string context = null)
        {
            if (!dict.TryGetValue(key, out var val) || !(val is Dictionary<string, object> obj))
                throw new FormatException($"Eksik veya geçersiz nesne '{key}'{(context != null ? " (" + context + ")" : "")}.");
            return obj;
        }

        private static List<object> RequireList(Dictionary<string, object> dict, string key, string context = null)
        {
            if (!dict.TryGetValue(key, out var val) || !(val is List<object> list))
                throw new FormatException($"Eksik veya geçersiz liste '{key}'{(context != null ? " (" + context + ")" : "")}.");
            return list;
        }

        private static string EscapeString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "\"\"";
            var sb = new StringBuilder();
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < ' ')
                            sb.AppendFormat("\\u{0:x4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        /// <summary>
        /// Pure C# JSON parser. Harici kütüphane veya Unity bağımlılığı olmadan JSON okur.
        /// Desteklenen tipler: Dictionary&lt;string, object&gt;, List&lt;object&gt;, string, long, double, bool, null.
        /// </summary>
        private static class JsonParser
        {
            public static object Parse(string json)
            {
                int index = 0;
                SkipWhitespace(json, ref index);
                var result = ParseValue(json, ref index);
                SkipWhitespace(json, ref index);
                if (index < json.Length)
                    throw new FormatException($"JSON sonunda beklenmeyen karakterler tespit edildi: konum {index}.");
                return result;
            }

            private static object ParseValue(string json, ref int index)
            {
                SkipWhitespace(json, ref index);
                if (index >= json.Length)
                    throw new FormatException("Beklenmeyen JSON sonu.");

                char c = json[index];
                if (c == '{')
                    return ParseObject(json, ref index);
                if (c == '[')
                    return ParseArray(json, ref index);
                if (c == '"')
                    return ParseString(json, ref index);
                if (c == 't' || c == 'f')
                    return ParseBool(json, ref index);
                if (c == 'n')
                    return ParseNull(json, ref index);
                if (c == '-' || char.IsDigit(c))
                    return ParseNumber(json, ref index);

                throw new FormatException($"JSON ayrıştırma hatası: geçersiz karakter '{c}' konum {index}.");
            }

            private static Dictionary<string, object> ParseObject(string json, ref int index)
            {
                var dict = new Dictionary<string, object>(StringComparer.Ordinal);
                index++; // skip '{'

                while (true)
                {
                    SkipWhitespace(json, ref index);
                    if (index >= json.Length)
                        throw new FormatException("Kapatılmamış JSON nesnesi ('}').");

                    if (json[index] == '}')
                    {
                        index++;
                        return dict;
                    }

                    if (json[index] != '"')
                        throw new FormatException($"Beklenen tırnak içindeki anahtar, bulundu '{json[index]}' konum {index}.");

                    string key = ParseString(json, ref index);
                    SkipWhitespace(json, ref index);

                    if (index >= json.Length || json[index] != ':')
                        throw new FormatException($"Anahtardan sonra ':' bekleniyordu: konum {index}.");
                    index++; // skip ':'

                    object value = ParseValue(json, ref index);
                    dict[key] = value;

                    SkipWhitespace(json, ref index);
                    if (index >= json.Length)
                        throw new FormatException("Kapatılmamış JSON nesnesi ('}').");

                    if (json[index] == ',')
                    {
                        index++;
                    }
                    else if (json[index] == '}')
                    {
                        index++;
                        return dict;
                    }
                    else
                    {
                        throw new FormatException($"Beklenen ',' veya '}}', bulundu '{json[index]}' konum {index}.");
                    }
                }
            }

            private static List<object> ParseArray(string json, ref int index)
            {
                var list = new List<object>();
                index++; // skip '['

                while (true)
                {
                    SkipWhitespace(json, ref index);
                    if (index >= json.Length)
                        throw new FormatException("Kapatılmamış JSON dizisi (']').");

                    if (json[index] == ']')
                    {
                        index++;
                        return list;
                    }

                    object value = ParseValue(json, ref index);
                    list.Add(value);

                    SkipWhitespace(json, ref index);
                    if (index >= json.Length)
                        throw new FormatException("Kapatılmamış JSON dizisi (']').");

                    if (json[index] == ',')
                    {
                        index++;
                    }
                    else if (json[index] == ']')
                    {
                        index++;
                        return list;
                    }
                    else
                    {
                        throw new FormatException($"Beklenen ',' veya ']', bulundu '{json[index]}' konum {index}.");
                    }
                }
            }

            private static string ParseString(string json, ref int index)
            {
                index++; // skip open quote
                var sb = new StringBuilder();

                while (index < json.Length)
                {
                    char c = json[index++];
                    if (c == '"')
                        return sb.ToString();

                    if (c == '\\')
                    {
                        if (index >= json.Length)
                            throw new FormatException("Beklenmeyen kaçış sonu.");
                        char esc = json[index++];
                        switch (esc)
                        {
                            case '"':
                                sb.Append('"');
                                break;
                            case '\\':
                                sb.Append('\\');
                                break;
                            case '/':
                                sb.Append('/');
                                break;
                            case 'b':
                                sb.Append('\b');
                                break;
                            case 'f':
                                sb.Append('\f');
                                break;
                            case 'n':
                                sb.Append('\n');
                                break;
                            case 'r':
                                sb.Append('\r');
                                break;
                            case 't':
                                sb.Append('\t');
                                break;
                            case 'u':
                                if (index + 4 > json.Length)
                                    throw new FormatException("Eksik unicode kaçış dizisi.");
                                string hex = json.Substring(index, 4);
                                index += 4;
                                sb.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                                break;
                            default:
                                throw new FormatException($"Bilinmeyen kaçış karakteri: '\\{esc}'.");
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                throw new FormatException("Kapatılmamış tırnak dizgisi.");
            }

            private static object ParseNumber(string json, ref int index)
            {
                int start = index;
                if (json[index] == '-')
                    index++;
                while (index < json.Length && char.IsDigit(json[index]))
                    index++;

                bool isFloating = false;
                if (index < json.Length && json[index] == '.')
                {
                    isFloating = true;
                    index++;
                    while (index < json.Length && char.IsDigit(json[index]))
                        index++;
                }

                if (index < json.Length && (json[index] == 'e' || json[index] == 'E'))
                {
                    isFloating = true;
                    index++;
                    if (index < json.Length && (json[index] == '+' || json[index] == '-'))
                        index++;
                    while (index < json.Length && char.IsDigit(json[index]))
                        index++;
                }

                string numStr = json.Substring(start, index - start);
                if (isFloating)
                {
                    return double.Parse(numStr, CultureInfo.InvariantCulture);
                }
                if (long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long integer))
                    return integer;
                throw new FormatException($"Tamsayı desteklenen aralığın dışında: '{numStr}'.");
            }

            private static bool ParseBool(string json, ref int index)
            {
                if (json.Substring(index).StartsWith("true"))
                {
                    index += 4;
                    return true;
                }
                if (json.Substring(index).StartsWith("false"))
                {
                    index += 5;
                    return false;
                }
                throw new FormatException($"Geçersiz boolean değeri: konum {index}.");
            }

            private static object ParseNull(string json, ref int index)
            {
                if (json.Substring(index).StartsWith("null"))
                {
                    index += 4;
                    return null;
                }
                throw new FormatException($"Geçersiz null değeri: konum {index}.");
            }

            private static void SkipWhitespace(string json, ref int index)
            {
                while (index < json.Length && char.IsWhiteSpace(json[index]))
                    index++;
            }
        }
    }
}

