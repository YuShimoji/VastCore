using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// UnityEngine 非依存の軽量 JSON パーサー（C# 9.0 / .NET / Unity 共通）。
    /// オブジェクトは Dictionary&lt;string, object&gt;、配列は List&lt;object&gt;、数値は double、文字列は string、真偽は bool、null は null を返す。
    /// </summary>
    public static class RegionJson
    {
        public static object Parse(string json)
        {
            if (json == null) throw new ArgumentNullException("json");
            var parser = new Parser(json);
            object value = parser.ParseValue();
            parser.SkipWhitespace();
            if (!parser.AtEnd)
                throw new FormatException("JSON の末尾に余分な文字があります (位置 " + parser.Index + ")");
            return value;
        }

        public static Dictionary<string, object> ParseObject(string json)
        {
            object value = Parse(json);
            var obj = value as Dictionary<string, object>;
            if (obj == null)
                throw new FormatException("JSON のルートはオブジェクトである必要があります。");
            return obj;
        }

        private sealed class Parser
        {
            private readonly string _s;
            public int Index;

            public Parser(string s)
            {
                _s = s;
            }

            public bool AtEnd => Index >= _s.Length;

            public void SkipWhitespace()
            {
                while (Index < _s.Length)
                {
                    char c = _s[Index];
                    if (c == ' ' || c == '\t' || c == '\r' || c == '\n') Index++;
                    else break;
                }
            }

            public object ParseValue()
            {
                SkipWhitespace();
                if (AtEnd) throw new FormatException("予期しない JSON の終端 (位置 " + Index + ")");
                char c = _s[Index];
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': return ParseLiteral("true", true);
                    case 'f': return ParseLiteral("false", false);
                    case 'n': return ParseLiteral("null", null);
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ParseNumber();
                        throw new FormatException("不正な JSON トークン '" + c + "' (位置 " + Index + ")");
                }
            }

            private Dictionary<string, object> ParseObject()
            {
                Index++; // '{'
                var dict = new Dictionary<string, object>(StringComparer.Ordinal);
                SkipWhitespace();
                if (!AtEnd && _s[Index] == '}')
                {
                    Index++;
                    return dict;
                }
                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || _s[Index] != '"')
                        throw new FormatException("オブジェクトのキー文字列が必要です (位置 " + Index + ")");
                    string key = ParseString();
                    SkipWhitespace();
                    if (AtEnd || _s[Index] != ':')
                        throw new FormatException("':' が必要です (位置 " + Index + ")");
                    Index++;
                    dict[key] = ParseValue();
                    SkipWhitespace();
                    if (AtEnd) throw new FormatException("オブジェクトが閉じられていません (位置 " + Index + ")");
                    if (_s[Index] == '}')
                    {
                        Index++;
                        return dict;
                    }
                    if (_s[Index] != ',')
                        throw new FormatException("',' または '}' が必要です (位置 " + Index + ")");
                    Index++;
                }
            }

            private List<object> ParseArray()
            {
                Index++; // '['
                var list = new List<object>();
                SkipWhitespace();
                if (!AtEnd && _s[Index] == ']')
                {
                    Index++;
                    return list;
                }
                while (true)
                {
                    list.Add(ParseValue());
                    SkipWhitespace();
                    if (AtEnd) throw new FormatException("配列が閉じられていません (位置 " + Index + ")");
                    if (_s[Index] == ']')
                    {
                        Index++;
                        return list;
                    }
                    if (_s[Index] != ',')
                        throw new FormatException("',' または ']' が必要です (位置 " + Index + ")");
                    Index++;
                }
            }

            private string ParseString()
            {
                Index++; // '"'
                var sb = new StringBuilder();
                while (Index < _s.Length)
                {
                    char c = _s[Index++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\')
                    {
                        sb.Append(c);
                        continue;
                    }
                    if (Index >= _s.Length)
                        throw new FormatException("不完全なエスケープ文字列 (位置 " + Index + ")");
                    char esc = _s[Index++];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (Index + 4 > _s.Length)
                                throw new FormatException("不正な \\u エスケープ (位置 " + Index + ")");
                            int code = int.Parse(_s.Substring(Index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                            sb.Append((char)code);
                            Index += 4;
                            break;
                        default:
                            throw new FormatException("不正なエスケープ '\\" + esc + "' (位置 " + Index + ")");
                    }
                }
                throw new FormatException("文字列が閉じられていません");
            }

            private object ParseLiteral(string literal, object result)
            {
                if (Index + literal.Length > _s.Length || string.CompareOrdinal(_s, Index, literal, 0, literal.Length) != 0)
                    throw new FormatException("不正なリテラル (位置 " + Index + ")");
                Index += literal.Length;
                return result;
            }

            private double ParseNumber()
            {
                int start = Index;
                if (_s[Index] == '-') Index++;
                while (Index < _s.Length && _s[Index] >= '0' && _s[Index] <= '9') Index++;
                if (Index < _s.Length && _s[Index] == '.')
                {
                    Index++;
                    while (Index < _s.Length && _s[Index] >= '0' && _s[Index] <= '9') Index++;
                }
                if (Index < _s.Length && (_s[Index] == 'e' || _s[Index] == 'E'))
                {
                    Index++;
                    if (Index < _s.Length && (_s[Index] == '+' || _s[Index] == '-')) Index++;
                    while (Index < _s.Length && _s[Index] >= '0' && _s[Index] <= '9') Index++;
                }
                string token = _s.Substring(start, Index - start);
                return double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }
    }
}
