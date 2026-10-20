using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DiamondMine.Simulation
{
    public abstract class JsonValue
    {
        public const int MaxTextLength = 1024 * 1024;
        public static JsonValue Parse(string text)
        {
            if (text == null)
                throw new FormatException("JSON text is missing.");
            if (text.Length > MaxTextLength)
                throw new FormatException("JSON exceeds the supported size.");
            var parser = new Parser(text);
            JsonValue value = parser.ParseValue();
            parser.Skip();
            if (!parser.End)
                throw new FormatException("Unexpected trailing JSON.");
            return value;
        }

        public virtual bool IsNull { get { return false; } }
        public virtual double AsNumber() { throw new FormatException("Expected a number."); }
        public virtual string AsString() { throw new FormatException("Expected a string."); }
        public virtual bool AsBool() { throw new FormatException("Expected a boolean."); }
        public virtual IReadOnlyList<JsonValue> AsArray() { throw new FormatException("Expected an array."); }
        public virtual IReadOnlyDictionary<string, JsonValue> AsObject() { throw new FormatException("Expected an object."); }

        public bool Has(string key)
        {
            JsonValue ignored;
            return AsObject().TryGetValue(key, out ignored);
        }

        public JsonValue Get(string key)
        {
            JsonValue value;
            if (!AsObject().TryGetValue(key, out value))
                throw new FormatException("Missing JSON field '" + key + "'.");
            return value;
        }

        public double Number(string key, double fallback)
        {
            JsonValue value;
            if (!AsObject().TryGetValue(key, out value) || value.IsNull)
                return fallback;
            return value.AsNumber();
        }

        public int Int(string key, int fallback)
        {
            JsonValue value;
            if (!AsObject().TryGetValue(key, out value) || value.IsNull)
                return fallback;
            return CheckedInt(value.AsNumber());
        }

        public static int CheckedInt(double value)
        {
            if (!NumericFormat.IsFinite(value) || value < int.MinValue || value > int.MaxValue || Math.Truncate(value) != value)
                throw new FormatException("Expected an integer in the supported range.");
            return (int)value;
        }

        public string Str(string key, string fallback)
        {
            JsonValue value;
            if (!AsObject().TryGetValue(key, out value) || value.IsNull)
                return fallback;
            return value.AsString();
        }

        public bool Bool(string key, bool fallback)
        {
            JsonValue value;
            if (!AsObject().TryGetValue(key, out value) || value.IsNull)
                return fallback;
            return value.AsBool();
        }

        public IReadOnlyList<JsonValue> Array(string key)
        {
            JsonValue value;
            if (!AsObject().TryGetValue(key, out value) || value.IsNull)
                return new JsonValue[0];
            return value.AsArray();
        }

        sealed class JsonNull : JsonValue
        {
            public static readonly JsonNull Instance = new JsonNull();
            public override bool IsNull { get { return true; } }
        }

        sealed class JsonBool : JsonValue
        {
            readonly bool value;
            public JsonBool(bool value) { this.value = value; }
            public override bool AsBool() { return value; }
        }

        sealed class JsonNumber : JsonValue
        {
            readonly double value;
            public JsonNumber(double value) { this.value = value; }
            public override double AsNumber() { return value; }
        }

        sealed class JsonString : JsonValue
        {
            readonly string value;
            public JsonString(string value) { this.value = value; }
            public override string AsString() { return value; }
        }

        sealed class JsonArray : JsonValue
        {
            readonly List<JsonValue> values;
            public JsonArray(List<JsonValue> values) { this.values = values; }
            public override IReadOnlyList<JsonValue> AsArray() { return values; }
        }

        sealed class JsonObject : JsonValue
        {
            readonly Dictionary<string, JsonValue> values;
            public JsonObject(Dictionary<string, JsonValue> values) { this.values = values; }
            public override IReadOnlyDictionary<string, JsonValue> AsObject() { return values; }
        }

        sealed class Parser
        {
            readonly string text;
            int index;
            int depth;

            public Parser(string text)
            {
                this.text = text;
            }

            public bool End { get { return index >= text.Length; } }

            public void Skip()
            {
                while (index < text.Length && char.IsWhiteSpace(text[index]))
                    index++;
            }

            public JsonValue ParseValue()
            {
                if (++depth > 64)
                    throw new FormatException("JSON nesting exceeds the supported depth.");
                try { return ParseValueCore(); }
                finally { depth--; }
            }

            JsonValue ParseValueCore()
            {
                Skip();
                if (index >= text.Length)
                    throw new FormatException("Unexpected end of JSON.");
                char c = text[index];
                if (c == '{') return ParseObject();
                if (c == '[') return ParseArray();
                if (c == '"') return new JsonString(ParseString());
                if (c == 't') { Expect("true"); return new JsonBool(true); }
                if (c == 'f') { Expect("false"); return new JsonBool(false); }
                if (c == 'n') { Expect("null"); return JsonNull.Instance; }
                if (c == '-' || (c >= '0' && c <= '9')) return new JsonNumber(ParseNumber());
                throw new FormatException("Unexpected JSON character at " + index + ".");
            }

            JsonObject ParseObject()
            {
                Expect("{");
                var map = new Dictionary<string, JsonValue>();
                Skip();
                if (Peek('}'))
                {
                    index++;
                    return new JsonObject(map);
                }
                while (true)
                {
                    Skip();
                    string key = ParseString();
                    Skip();
                    Expect(":");
                    if (map.ContainsKey(key))
                        throw new FormatException("Duplicate JSON property.");
                    map.Add(key, ParseValue());
                    Skip();
                    if (Peek('}'))
                    {
                        index++;
                        break;
                    }
                    Expect(",");
                }
                return new JsonObject(map);
            }

            JsonArray ParseArray()
            {
                Expect("[");
                var list = new List<JsonValue>();
                Skip();
                if (Peek(']'))
                {
                    index++;
                    return new JsonArray(list);
                }
                while (true)
                {
                    list.Add(ParseValue());
                    Skip();
                    if (Peek(']'))
                    {
                        index++;
                        break;
                    }
                    Expect(",");
                }
                return new JsonArray(list);
            }

            string ParseString()
            {
                if (index >= text.Length || text[index] != '"')
                    throw new FormatException("Expected a string.");
                index++;
                var sb = new StringBuilder();
                while (index < text.Length)
                {
                    char c = text[index++];
                    if (c == '"')
                        return sb.ToString();
                    if (c == '\\')
                    {
                        if (index >= text.Length)
                            throw new FormatException("Bad escape.");
                        char e = text[index++];
                        if (e == '"' || e == '\\' || e == '/') sb.Append(e);
                        else if (e == 'b') sb.Append('\b');
                        else if (e == 'f') sb.Append('\f');
                        else if (e == 'n') sb.Append('\n');
                        else if (e == 'r') sb.Append('\r');
                        else if (e == 't') sb.Append('\t');
                        else if (e == 'u')
                        {
                            if (index + 4 > text.Length)
                                throw new FormatException("Bad unicode escape.");
                            int code = int.Parse(text.Substring(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                            sb.Append((char)code);
                            index += 4;
                        }
                        else throw new FormatException("Unknown escape.");
                    }
                    else
                    {
                        if (c < 32) throw new FormatException("Unescaped control character in JSON string.");
                        sb.Append(c);
                    }
                }
                throw new FormatException("Unterminated string.");
            }

            double ParseNumber()
            {
                int start = index;
                if (Peek('-')) index++;
                if (Peek('0'))
                {
                    index++;
                    if (Digit()) throw new FormatException("Leading zero in JSON number.");
                }
                else
                {
                    int digits = index;
                    while (Digit()) index++;
                    if (index == digits) throw new FormatException("Missing JSON number digits.");
                }
                if (Peek('.'))
                {
                    index++;
                    int digits = index;
                    while (Digit()) index++;
                    if (index == digits) throw new FormatException("Missing fraction digits.");
                }
                if (index < text.Length && (text[index] == 'e' || text[index] == 'E'))
                {
                    index++;
                    if (index < text.Length && (text[index] == '+' || text[index] == '-')) index++;
                    int digits = index;
                    while (Digit()) index++;
                    if (index == digits) throw new FormatException("Missing exponent digits.");
                }
                string slice = text.Substring(start, index - start);
                double result;
                if (!double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out result) || !NumericFormat.IsFinite(result))
                    throw new FormatException("JSON number exceeds the supported range.");
                return result;
            }

            bool Digit() { return index < text.Length && text[index] >= '0' && text[index] <= '9'; }

            void Expect(string token)
            {
                Skip();
                if (index + token.Length > text.Length || text.Substring(index, token.Length) != token)
                    throw new FormatException("Expected '" + token + "'.");
                index += token.Length;
            }

            bool Peek(char c)
            {
                return index < text.Length && text[index] == c;
            }
        }
    }

    public sealed class JsonComposer
    {
        readonly StringBuilder sb = new StringBuilder();
        readonly Stack<Scope> scopes = new Stack<Scope>();

        sealed class Scope
        {
            public bool First = true;
            public bool AwaitValue;
        }

        public void BeginObject() { Begin('{'); }
        public void EndObject() { End('}'); }
        public void BeginArray() { Begin('['); }
        public void EndArray() { End(']'); }

        public void Name(string name)
        {
            WritePrefix();
            WriteString(name);
            sb.Append(':');
            scopes.Peek().AwaitValue = true;
        }

        public void Value(string value)
        {
            WritePrefix();
            WriteString(value ?? "");
        }

        public void Value(bool value)
        {
            WritePrefix();
            sb.Append(value ? "true" : "false");
        }

        public void Value(int value)
        {
            WritePrefix();
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        public void Value(double value)
        {
            if (!NumericFormat.IsFinite(value))
                throw new InvalidOperationException("Refusing to write a non-finite number.");
            WritePrefix();
            sb.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        public void Prop(string name, string value) { Name(name); Value(value); }
        public void Prop(string name, bool value) { Name(name); Value(value); }
        public void Prop(string name, int value) { Name(name); Value(value); }
        public void Prop(string name, double value) { Name(name); Value(value); }

        public override string ToString()
        {
            return sb.ToString();
        }

        void Begin(char bracket)
        {
            WritePrefix();
            sb.Append(bracket);
            scopes.Push(new Scope());
        }

        void End(char bracket)
        {
            if (scopes.Count == 0)
                throw new InvalidOperationException("JSON container underflow.");
            scopes.Pop();
            sb.Append(bracket);
        }

        void WritePrefix()
        {
            if (scopes.Count == 0)
                return;
            Scope scope = scopes.Peek();
            if (scope.AwaitValue)
            {
                scope.AwaitValue = false;
                return;
            }
            if (!scope.First)
                sb.Append(',');
            scope.First = false;
        }

        static void WriteString(StringBuilder sb, string value)
        {
            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else if (c < 32) sb.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                else sb.Append(c);
            }
            sb.Append('"');
        }

        void WriteString(string value)
        {
            WriteString(sb, value);
        }
    }
}
