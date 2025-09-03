using System;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.IO;
using Json.Path;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Farrier.Models.Conditions
{
    class JsonQueryCondition : BaseCondition
    {
        public new static JsonQueryCondition FromNode(XmlNode conditionNode)
        {
            return new JsonQueryCondition(conditionNode);
        }

        public JsonQueryCondition(XmlNode conditionNode) : base(conditionNode)
        {
            rawPath = XmlHelper.XmlAttributeToString(conditionNode.Attributes["path"]);
            rawMatchCase = XmlHelper.XmlAttributeToString(conditionNode.Attributes["matchcase"]);
            rawValue = XmlHelper.XmlAttributeToString(conditionNode.Attributes["value"]);
            Query = XmlHelper.XmlAttributeToString(conditionNode.Attributes["query"]);
            rawComparison = XmlHelper.XmlAttributeToString(conditionNode.Attributes["comparison"]);
            if(string.IsNullOrEmpty(rawComparison))
            {
                rawComparison = "equals";
            }
            rawMin = XmlHelper.XmlAttributeToString(conditionNode.Attributes["min"]);
            rawMax = XmlHelper.XmlAttributeToString(conditionNode.Attributes["max"]);
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule = null, InspectionRule parentRule = null, int prefix = 0, string startingpath = "")
        {
            messages.Clear();
            propertyMap.Clear();
            var potentialSuppressions = parentRule.GetSuppressionsForCondition(this);

            var comparison = tokens.DecodeString(rawComparison);
            if (comparison != "equals" && comparison != "notequals" && comparison != "count" && comparison != "contains" && comparison != "notcontains" && comparison != "matches" && comparison != "notmatches" && comparison != "length")
            {
                setFailureMessage(tokens, $"Invalid Json Query comparison value ({comparison})", potentialSuppressions);
                return false;
            }

            var path = PathNormalizer.Normalize(Path.Combine(startingpath, tokens.DecodeString(rawPath)));
            propertyMap.Add("path", path);
            if (File.Exists(path))
            {
                JsonPath parsedPath;
                if(JsonPath.TryParse(Query, out parsedPath))
                {
                    var content = File.ReadAllText(path).Replace("\r\n", "\n").Replace('\r', '\n');
                    if(string.IsNullOrEmpty(content))
                    {
                        setFailureMessage(tokens, $"Can't query inside an empty document! (path: {path})", potentialSuppressions);
                        return false;
                    }
                    JsonDocument doc;
                    try
                    {
                        doc = JsonDocument.Parse(content);
                    }
                    catch (JsonException ex)
                    {
                        messages.Add(new Message(MessageLevel.error, Name, $"Unable to parse JSON File {path}! Details: {ex.Message}", prefix));
                        return false;
                    }

                    var queryValue = parsedPath.Evaluate(doc.RootElement);
                    if(!string.IsNullOrEmpty(queryValue.Error))
                    {
                        setFailureMessage(tokens, $"Error processing Json Query: {queryValue.Error}", potentialSuppressions);
                        return false;
                    }
                    else
                    {
                        var matchcase = tokens.DecodeString(rawMatchCase) == "true";
                        var value = tokens.DecodeString(rawValue);
                        switch(comparison)
                        {
                            case "count":
                                var count = queryValue.Matches.Count;
                                //Remove blank string results
                                foreach (var match in queryValue.Matches)
                                {
                                    if(match.Value.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(match.Value.GetString()))
                                        count -= 1;
                                }
                                if (!string.IsNullOrEmpty(rawValue))
                                {
                                    int numValue;
                                    if (!int.TryParse(rawValue, out numValue))
                                    {
                                        setFailureMessage(tokens, $"If value is included when comparison count, it must be a number ({rawValue})", potentialSuppressions);
                                        return false;
                                    }
                                    if (numValue != count)
                                    {
                                        setFailureMessage(tokens, $"Query result count is not equal (got {count} expected {numValue})", potentialSuppressions);
                                        return false;
                                    }
                                    else
                                    {
                                        return true;
                                    }
                                }
                                else
                                {
                                    bool minOK = false;
                                    bool maxOK = false;
                                    if (!string.IsNullOrEmpty(rawMin))
                                    {
                                        int min;
                                        if (!int.TryParse(rawMin, out min))
                                        {
                                            setFailureMessage(tokens, $"If min is included when comparison count, it must be a number ({rawMin})", potentialSuppressions);
                                            return false;
                                        }
                                        if (min > count)
                                        {
                                            setFailureMessage(tokens, $"Query result count is under min (got {count} min {min})", potentialSuppressions);
                                            return false;
                                        }
                                        else
                                        {
                                            minOK = true;
                                        }
                                    }
                                    else
                                    {
                                        minOK = true;
                                    }

                                    if (!string.IsNullOrEmpty(rawMax))
                                    {
                                        int max;
                                        if (!int.TryParse(rawMax, out max))
                                        {
                                            setFailureMessage(tokens, $"If max is included when comparison count, it must be a number ({rawMax})", potentialSuppressions);
                                            return false;
                                        }
                                        if (max < count)
                                        {
                                            setFailureMessage(tokens, $"Query result count is over max (got {count} max {max})", potentialSuppressions);
                                            return false;
                                        }
                                        else
                                        {
                                            maxOK = true;
                                        }
                                    }
                                    else
                                    {
                                        maxOK = true;
                                    }

                                    if (minOK && maxOK)
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        setFailureMessage(tokens, $"Query result count out of range ({count})", potentialSuppressions);
                                        return false;
                                    }
                                }
                            case "contains":
                            case "notcontains":
                                if (comparison == "contains" && queryValue.Matches.Count == 0)
                                {
                                    setFailureMessage(tokens, $"Query found no matches, so nothing to compare against", potentialSuppressions);
                                    return false;
                                }
                                var contains = false;
                                foreach(var match in queryValue.Matches)
                                {
                                    if (match.Value.GetString().Contains(value, (matchcase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase)))
                                    {
                                        contains = true;
                                        break;
                                    }
                                }
                                return comparison == "contains" ? contains : !contains;
                            case "matches":
                            case "notmatches":
                                var matches = false;
                                foreach (var result in queryValue.Matches)
                                {
                                    if (result.Value.ValueKind == JsonValueKind.String && Regex.IsMatch(result.Value.GetString(), value))
                                        matches = true;
                                }
                                if ((matches && comparison == "matches") || (!matches && comparison == "notmatches"))
                                    return true;
                                else
                                {
                                    setFailureMessage(tokens, $"Pattern {(comparison == "matches" ? "doesn't match" : "matches")} query result", potentialSuppressions);
                                    return false;
                                }
                            case "length":
                                var length = queryValue.Matches.Count > 0 ? queryValue.Matches[0].Value.ToString().Length : 0;
                                if (!string.IsNullOrEmpty(rawValue))
                                {
                                    int numValue;
                                    if (!int.TryParse(rawValue, out numValue))
                                    {
                                        setFailureMessage(tokens, $"If value is included when comparison length, it must be a number ({rawValue})", potentialSuppressions);
                                        return false;
                                    }
                                    if (numValue != length)
                                    {
                                        setFailureMessage(tokens, $"Query result length is not equal (got {length} expected {numValue})", potentialSuppressions);
                                        return false;
                                    }
                                    else
                                    {
                                        return true;
                                    }
                                }
                                else
                                {
                                    bool minOK = false;
                                    bool maxOK = false;
                                    if (!string.IsNullOrEmpty(rawMin))
                                    {
                                        int min;
                                        if (!int.TryParse(rawMin, out min))
                                        {
                                            setFailureMessage(tokens, $"If min is included when comparison length, it must be a number ({rawMin})", potentialSuppressions);
                                            return false;
                                        }
                                        if (min > length)
                                        {
                                            setFailureMessage(tokens, $"Query result length is under min (got {length} min {min})", potentialSuppressions);
                                            return false;
                                        }
                                        else
                                        {
                                            minOK = true;
                                        }
                                    }
                                    else
                                    {
                                        minOK = true;
                                    }

                                    if (!string.IsNullOrEmpty(rawMax))
                                    {
                                        int max;
                                        if (!int.TryParse(rawMax, out max))
                                        {
                                            setFailureMessage(tokens, $"If max is included when comparison length, it must be a number ({rawMax})", potentialSuppressions);
                                            return false;
                                        }
                                        if (max < length)
                                        {
                                            setFailureMessage(tokens, $"Query result length is over max (got {length} max {max})", potentialSuppressions);
                                            return false;
                                        }
                                        else
                                        {
                                            maxOK = true;
                                        }
                                    }
                                    else
                                    {
                                        maxOK = true;
                                    }

                                    if (minOK && maxOK)
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        setFailureMessage(tokens, $"Query result length out of range ({length})", potentialSuppressions);
                                        return false;
                                    }
                                }
                            default:
                                //Equals/NotEquals
                                if(queryValue.Matches.Count == 0)
                                {
                                    if(comparison == "equals")
                                    {
                                        setFailureMessage(tokens, $"Query found no matches, so nothing to compare against", potentialSuppressions);
                                        return false;
                                    }
                                    return true; //notequals
                                }
                                string queryResult = queryValue.Matches[0].Value.GetString();
                                if (value.Equals(queryResult, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    if (matchcase && !value.Equals(queryResult))
                                    {
                                        if (comparison == "equals")
                                        {
                                            //Invalid casing
                                            setFailureMessage(tokens, $"Value found but casing does not match (Result: {queryResult})", potentialSuppressions);
                                            return false;
                                        }
                                        return true; //notequals
                                    }
                                    else
                                    {
                                        if (comparison == "equals")
                                            return true;
                                        else
                                            return false; //notequals
                                    }
                                }
                                else
                                {
                                    if (comparison == "equals")
                                    {
                                        setFailureMessage(tokens, $"Queried Value does not match (Result: '{queryResult}', Expected: '{value}')", potentialSuppressions);
                                        return false;
                                    }
                                    return true; //notequals
                                }
                        }
                    }
                }
                else
                {
                    //Invalid query
                    setFailureMessage(tokens, "Invalid Json Query", potentialSuppressions);
                    return false;
                }
            }
            else
            {
                setFailureMessage(tokens, $"File not found at {path}", potentialSuppressions);
                return false;
            }
        }

        private string rawPath;
        private string rawMatchCase;
        private string rawComparison;
        private string Query; //Tokens not supported
        private string rawValue;
        private string rawMin;
        private string rawMax;
    }
}
