using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MCP.Editor
{
    /// <summary>
    /// Minimal JSON encoder/decoder for Unity editor tooling.
    /// Based on the MIT-licensed MiniJSON implementation by Calvin Rien.
    /// </summary>
    public static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return Parser.Parse(json);
        }

        public static string Serialize(object obj)
        {
            return Serializer.Serialize(obj);
        }

        private sealed class Parser : IDisposable
        {
            private readonly StringReader _reader;

            private Parser(string json)
            {
                _reader = new StringReader(json);
            }

            public static object Parse(string json)
            {
                using var instance = new Parser(json);
                return instance.ParseValue();
            }

            public void Dispose()
            {
                _reader.Dispose();
            }

            private enum Token
            {
                None,
                CurlyOpen,
                CurlyClose,
                SquaredOpen,
                SquaredClose,
                Colon,
                Comma,
                String,
                Number,
                True,
                False,
                Null,
            }

            private Dictionary<string, object> ParseObject()
            {
                var table = new Dictionary<string, object>(StringComparer.Ordinal);

                // ditch opening brace
                _reader.Read();

                while (true)
                {
                    var nextToken = NextToken;
                    switch (nextToken)
                    {
                        case Token.None:
                            return null;
                        case Token.Comma:
                            continue;
                        case Token.CurlyClose:
                            return table;
                        default:
                        {
                            var name = ParseString();
                            if (name == null)
                            {
                                return null;
                            }

                            if (NextToken != Token.Colon)
                            {
                                return null;
                            }

                            // ditch the colon
                            _reader.Read();
                            table[name] = ParseValue();
                            break;
                        }
                    }
                }
            }

            private List<object> ParseArray()
            {
                var array = new List<object>();

                // ditch opening bracket
                _reader.Read();

                var parsing = true;
                while (parsing)
                {
                    var nextToken = NextToken;

                    switch (nextToken)
                    {
                        case Token.None:
                            return null;
                        case Token.Comma:
                            continue;
                        case Token.SquaredClose:
                            parsing = false;
                            break;
                        default:
                            array.Add(ParseValueByToken(nextToken));
                            break;
                    }
                }

                return array;
            }

            private object ParseValue()
            {
                var nextToken = NextToken;
                return ParseValueByToken(nextToken);
            }

            private object ParseValueByToken(Token token)
            {
                return token switch
                {
                    Token.String => ParseString(),
                    Token.Number => ParseNumber(),
                    Token.CurlyOpen => ParseObject(),
                    Token.SquaredOpen => ParseArray(),
                    Token.True => true,
                    Token.False => false,
                    Token.Null => null,
                    _ => null,
                };
            }

            private string ParseString()
            {
                var sb = new StringBuilder();

                // ditch opening quote
                _reader.Read();

                while (true)
                {
                    if (PeekChar == -1)
                    {
                        break;
                    }

                    var ch = NextChar;
                    switch (ch)
                    {
                        case '"':
                            return sb.ToString();
                        case '\\':
                        {
                            if (PeekChar == -1)
                            {
                                return null;
                            }

                            ch = NextChar;
                            switch (ch)
                            {
                                case '"':
                                case '\\':
                                case '/':
                                    sb.Append(ch);
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
                                {
                                    var hex = new char[4];
                                    var read = _reader.Read(hex, 0, 4);
                                    if (read < 4)
                                    {
                                        return null;
                                    }

                                    sb.Append((char)Convert.ToInt32(new string(hex), 16));
                                    break;
                                }
                            }

                            break;
                        }
                        default:
                            sb.Append(ch);
                            break;
                    }
                }

                return null;
            }

            private object ParseNumber()
            {
                var number = NextWord;

                if (number.IndexOf('.', StringComparison.Ordinal) != -1)
                {
                    if (double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                    {
                        return parsed;
                    }

                    return null;
                }

                if (long.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
                {
                    return integer;
                }

                return null;
            }

            private void EatWhitespace()
            {
                while (char.IsWhiteSpace(PeekChar))
                {
                    _ = NextChar;
                    if (PeekChar == -1)
                    {
                        break;
                    }
                }
            }

            private char PeekChar => Convert.ToChar(_reader.Peek());

            private char NextChar => Convert.ToChar(_reader.Read());

            private Token NextToken
            {
                get
                {
                    EatWhitespace();

                    if (_reader.Peek() == -1)
                    {
                        return Token.None;
                    }

                    var ch = PeekChar;
                    switch (ch)
                    {
                        case '{':
                            return Token.CurlyOpen;
                        case '}':
                            _reader.Read();
                            return Token.CurlyClose;
                        case '[':
                            return Token.SquaredOpen;
                        case ']':
                            _reader.Read();
                            return Token.SquaredClose;
                        case ',':
                            _reader.Read();
                            return Token.Comma;
                        case '"':
                            return Token.String;
                        case ':':
                            return Token.Colon;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '-':
                            return Token.Number;
                    }

                    var word = NextWord;
                    return word switch
                    {
                        "false" => Token.False,
                        "true" => Token.True,
                        "null" => Token.Null,
                        _ => Token.None,
                    };
                }
            }

            private string NextWord
            {
                get
                {
                    var sb = new StringBuilder();

                    while (!IsWordBreak(PeekChar))
                    {
                        sb.Append(NextChar);
                        if (_reader.Peek() == -1)
                        {
                            break;
                        }
                    }

                    return sb.ToString();
                }
            }

            private static bool IsWordBreak(char c)
            {
                return char.IsWhiteSpace(c) || c == ',' || c == '}' || c == ']' || c == ':' || c == '"';
            }
        }

        private sealed class Serializer
        {
            private readonly StringBuilder _builder = new();

            private Serializer()
            {
            }

            public static string Serialize(object obj)
            {
                var instance = new Serializer();
                instance.SerializeValue(obj);
                return instance._builder.ToString();
            }

            private void SerializeValue(object value)
            {
                switch (value)
                {
                    case null:
                        _builder.Append("null");
                        break;
                    case string s:
                        SerializeString(s);
                        break;
                    case bool b:
                        _builder.Append(b ? "true" : "false");
                        break;
                    case IDictionary<string, object> dict:
                        SerializeObject(dict);
                        break;
                    case IDictionary dictionary:
                        SerializeDictionary(dictionary);
                        break;
                    case IList<object> list:
                        SerializeArrayGeneric(list);
                        break;
                    case IList list:
                        SerializeArray(list);
                        break;
                    case char ch:
                        SerializeString(new string(ch, 1));
                        break;
                    case IFormattable format:
                        _builder.Append(format.ToString(null, CultureInfo.InvariantCulture));
                        break;
                    default:
                        SerializeString(value.ToString() ?? string.Empty);
                        break;
                }
            }

            private void SerializeObject(IDictionary<string, object> obj)
            {
                var first = true;
                _builder.Append('{');

                foreach (var pair in obj)
                {
                    if (!first)
                    {
                        _builder.Append(',');
                    }

                    SerializeString(pair.Key);
                    _builder.Append(':');
                    SerializeValue(pair.Value);
                    first = false;
                }

                _builder.Append('}');
            }

            private void SerializeDictionary(IDictionary dict)
            {
                var first = true;
                _builder.Append('{');
                foreach (DictionaryEntry entry in dict)
                {
                    if (!first)
                    {
                        _builder.Append(',');
                    }

                    SerializeString(Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty);
                    _builder.Append(':');
                    SerializeValue(entry.Value);
                    first = false;
                }

                _builder.Append('}');
            }

            private void SerializeArray(IList array)
            {
                var first = true;
                _builder.Append('[');

                foreach (var obj in array)
                {
                    if (!first)
                    {
                        _builder.Append(',');
                    }

                    SerializeValue(obj);
                    first = false;
                }

                _builder.Append(']');
            }

            private void SerializeArrayGeneric(IList<object> array)
            {
                var first = true;
                _builder.Append('[');

                foreach (var obj in array)
                {
                    if (!first)
                    {
                        _builder.Append(',');
                    }

                    SerializeValue(obj);
                    first = false;
                }

                _builder.Append(']');
            }

            private void SerializeString(string str)
            {
                _builder.Append('"');

                foreach (var c in str)
                {
                    switch (c)
                    {
                        case '"':
                            _builder.Append("\\\"");
                            break;
                        case '\\':
                            _builder.Append("\\\\");
                            break;
                        case '\b':
                            _builder.Append("\\b");
                            break;
                        case '\f':
                            _builder.Append("\\f");
                            break;
                        case '\n':
                            _builder.Append("\\n");
                            break;
                        case '\r':
                            _builder.Append("\\r");
                            break;
                        case '\t':
                            _builder.Append("\\t");
                            break;
                        default:
                            if (c < ' ')
                            {
                                _builder.Append("\\u");
                                _builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                _builder.Append(c);
                            }

                            break;
                    }
                }

                _builder.Append('"');
            }
        }
    }
}
