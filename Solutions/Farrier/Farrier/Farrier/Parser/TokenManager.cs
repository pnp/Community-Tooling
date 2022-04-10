using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using System.Data;

namespace Farrier.Parser
{
    class TokenManager
    {
        public string TOKENSTART { get; }
        public string TOKENEND { get; }
        public string COLUMNSTART { get; }
        public string COLUMNEND { get; }

        private LogRouter _log;

        private FunctionResolver _functionResolver;

        private Dictionary<string, string> _tokens;

        public TokenManager(FunctionResolver fr, string start = "@@", string end = "@@", string columnStart = "[", string columnEnd = "]", LogRouter log = null)
        {
            TOKENSTART = start;
            TOKENEND = end;
            COLUMNSTART = columnStart;
            COLUMNEND = columnEnd;

            if (log == null)
                _log = new LogRouter();
            else
                _log = log;

            _functionResolver = fr;

            _tokens = new Dictionary<string, string>();
            AddToken("Now", DateTime.Now.ToString("F"));
        }

        public void Reset()
        {
            _tokens = new Dictionary<string, string>();
            AddToken("Now", DateTime.Now.ToString("F"));
        }

        public TokenManager(TokenManager baseTokens)
        {
            TOKENSTART = baseTokens.TOKENSTART;
            TOKENEND = baseTokens.TOKENEND;
            COLUMNSTART = baseTokens.COLUMNSTART;
            COLUMNEND = baseTokens.COLUMNEND;
            _log = baseTokens._log;

            _functionResolver = baseTokens._functionResolver;

            _tokens = new Dictionary<string, string>(baseTokens._tokens);
        }

        public static List<TokenManager> TokenizeRows(DataTable dt, FunctionResolver fr, string start = "@@", string end = "@@", string columnStart = "[", string columnEnd = "]", LogRouter log = null)
        {
            List<DataRow> rows = new List<DataRow>();
            foreach (DataRow row in dt.Rows)
            {
                rows.Add(row);
            }
            return TokenizeRows(rows, dt.Columns, fr, start, end, columnStart, columnEnd, log);
        }

        public static List<TokenManager> TokenizeRows(List<DataRow> rows, DataColumnCollection cols, FunctionResolver fr, string start = "@@", string end = "@@", string columnStart = "[", string columnEnd = "]", LogRouter log = null)
        {
            var rowTokens = new List<TokenManager>();
            int rowIndex = 0;
            foreach (DataRow row in rows)
            {
                var tokens = TokenizeRow(row, cols, fr, start, end, columnStart, columnEnd, log);

                tokens.AddToken("ItemIndex", rowIndex.ToString());
                tokens.AddToken("IsFirstItem", (rowIndex == 0) ? "true" : "false");
                tokens.AddToken("IsLastItem", (rowIndex == rows.Count - 1) ? "true" : "false");
                rowIndex += 1;

                rowTokens.Add(tokens);
            }

            return rowTokens;
        }

        public static TokenManager TokenizeRow(DataRow row, DataColumnCollection cols, FunctionResolver fr, string start = "@@", string end = "@@", string columnStart = "[", string columnEnd = "]", LogRouter log = null)
        {
            var tokens = new TokenManager(fr, start, end, columnStart, columnEnd, log);

            foreach (DataColumn col in cols)
            {
                tokens.AddToken(col.ColumnName, row[col].ToString(), true);
            }

            return tokens;
        }


        public void AddToken(string key, string value, bool asColumn = false, params TokenManager[] additionalTokens)
        {
            string tokenKey = buildKey(key, asColumn);
            if(!_tokens.ContainsKey(tokenKey))
            {
                _tokens.Add(tokenKey, DecodeString(value, false, additionalTokens));
            }
        }

        public void NestToken(string key, string value, bool asColumn = false, int count = 1, params TokenManager[] additionalTokens)
        {
            //Adds if it doesn't exist, otherwise bumps up the previous with a number (key, key2, key3, etc.)
            string tokenKey = buildKey((count > 1 ? key + count.ToString() : key), asColumn);
            if (!_tokens.ContainsKey(tokenKey))
            {
                if (count == 0)
                {
                    _tokens.Add(tokenKey, DecodeString(value, false, additionalTokens));
                }
                else
                {
                    //Take the value exactly as it is (will have been previously decoded since this is just a bump)
                    _tokens.Add(tokenKey, value);
                }
            }
            else
            {
                var bumpedvalue = _tokens[tokenKey];
                if(count == 1)
                {
                    _tokens[tokenKey] = DecodeString(value, false, additionalTokens);
                }
                else
                {
                    //Take the value exactly as it is (will have been previously decoded since this is just a bump)
                    _tokens[tokenKey] = value;
                }
                
                //bump the previous
                NestToken(key, bumpedvalue, asColumn, count + 1, additionalTokens);
            }
        }

        public void AddTokens(IEnumerable<string> strings, bool asColumns = false, params TokenManager[] additionalTokens)
        {
            AddTokens(IEnumerableToDictionary(strings), asColumns, additionalTokens);
        }

        public void AddTokens(Dictionary<string, string> dictionary, bool asColumns = false, params TokenManager[] additionalTokens)
        {
            if (dictionary != null && dictionary.Count > 0)
            {
                foreach (var entry in dictionary)
                {
                    AddToken(entry.Key, entry.Value, asColumns, additionalTokens);
                }
            }
        }

        public void AddTokens(XmlNode tokensNode, bool asColumns = false, params TokenManager[] additionalTokens)
        {
            if (tokensNode != null && tokensNode.ChildNodes.Count > 0)
            {
                foreach (XmlNode tokenNode in tokensNode)
                {
                    if (tokenNode.NodeType == XmlNodeType.Element)
                    {
                        XmlAttribute nameAttribute = tokenNode.Attributes["name"];
                        XmlAttribute valueAttribute = tokenNode.Attributes["value"];
                        if (nameAttribute != null && !String.IsNullOrEmpty(nameAttribute.Value) && valueAttribute != null && !String.IsNullOrEmpty(valueAttribute.Value))
                        {
                            AddToken(nameAttribute.Value, valueAttribute.Value, asColumns, additionalTokens);
                        }
                    }
                }
            }
        }

        public void LogTokens(int prefix = 0)
        {
            foreach(var token in _tokens)
            {
                _log.Debug($"Token: {token.Key} = \"{token.Value}\"", prefix);
            }
        }

        public string DecodeString(string encodedValue, bool skipFunctions = false, params TokenManager[] additionalTokens)
        {
            foreach (TokenManager additionalToken in additionalTokens)
            {
                encodedValue = additionalToken.DecodeString(encodedValue, true);
            }
            foreach (KeyValuePair<string, string> tokenKVP in _tokens)
            {
                encodedValue = encodedValue.Replace(tokenKVP.Key, tokenKVP.Value);
            }
            if (skipFunctions)
                return encodedValue;
            else
                return _functionResolver.ResolveFunctions(encodedValue);
        }


        private string buildKey(string key, bool asColumn = false)
        {
            if (asColumn)
            {
                return TOKENSTART + COLUMNSTART + key + COLUMNEND + TOKENEND;
            }
            else
            {
                return TOKENSTART + key + TOKENEND;
            }
        }

        private string stripKey(string rawKey)
        {
            if (rawKey.StartsWith(TOKENSTART))
            {
                rawKey = rawKey.Substring(TOKENSTART.Length);
                if (rawKey.StartsWith(COLUMNSTART))
                {
                    rawKey = rawKey.Substring(COLUMNSTART.Length);
                }
            }
            if (rawKey.EndsWith(TOKENEND))
            {
                rawKey = rawKey.Substring(0, rawKey.Length - TOKENEND.Length);
                if (rawKey.EndsWith(COLUMNEND))
                {
                    rawKey = rawKey.Substring(0, rawKey.Length - COLUMNEND.Length);
                }
            }
            return rawKey;
        }

        public Dictionary<string, string> CleanTokens()
        {
            var cleanTokens = new Dictionary<string, string>();
            foreach (var token in _tokens)
            {
                cleanTokens.Add(stripKey(token.Key), token.Value);
            }
            return cleanTokens;
        }

        public static Dictionary<string, string> IEnumerableToDictionary(IEnumerable<string> strings)
        {
            var dictionary = new Dictionary<string, string>();

            var values = new List<string>();
            foreach (var s in strings)
            {
                values.Add(s);
                if (values.Count % 2 == 0)
                {
                    dictionary.Add(values[values.Count - 2], values[values.Count - 1]);
                }
            }

            return dictionary;
        }
    }
}
