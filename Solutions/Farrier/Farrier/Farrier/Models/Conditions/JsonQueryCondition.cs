using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.IO;
using Json.Path;
using System.Text.Json;

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
            if(String.IsNullOrEmpty(rawComparison))
            {
                rawComparison = "equals";
            }
            rawMin = XmlHelper.XmlAttributeToString(conditionNode.Attributes["min"]);
            rawMax = XmlHelper.XmlAttributeToString(conditionNode.Attributes["max"]);
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule = null, InspectionRule parentRule = null, int prefix = 0, string startingpath = "")
        {
            messages.Clear();
            var comparison = tokens.DecodeString(rawComparison);
            if (comparison != "equals" && comparison != "count" && comparison != "contains" && comparison != "notcontains")
            {
                this.setFailureMessage(tokens, $"Invalid Json Query comparison value ({comparison})");
                return false;
            }

            var path = System.IO.Path.Combine(startingpath, tokens.DecodeString(rawPath));
            if (File.Exists(path))
            {
                JsonPath parsedPath;
                if(JsonPath.TryParse(Query, out parsedPath))
                {
                    var content = File.ReadAllText(path);
                    if(String.IsNullOrEmpty(content))
                    {
                        this.setFailureMessage(tokens, $"Can't query inside an empty document! (path: {path})");
                        return false;
                    }
                    var doc = JsonDocument.Parse(content);

                    var queryValue = parsedPath.Evaluate(doc.RootElement);
                    if(!String.IsNullOrEmpty(queryValue.Error))
                    {
                        this.setFailureMessage(tokens, $"Error processing Json Query: {queryValue.Error}");
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
                                    if(match.Value.ValueKind == JsonValueKind.String && String.IsNullOrEmpty(match.Value.GetString()))
                                        count -= 1;
                                }
                                if (!String.IsNullOrEmpty(rawValue))
                                {
                                    int numValue;
                                    if (!int.TryParse(rawValue, out numValue))
                                    {
                                        this.setFailureMessage(tokens, $"If value is included when comparison count, it must be a number ({rawValue})");
                                        return false;
                                    }
                                    if (numValue != count)
                                    {
                                        this.setFailureMessage(tokens, $"Query result count is not equal (got {count} expected {numValue})");
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
                                    if (!String.IsNullOrEmpty(rawMin))
                                    {
                                        int min;
                                        if (!int.TryParse(rawMin, out min))
                                        {
                                            this.setFailureMessage(tokens, $"If min is included when comparison count, it must be a number ({rawMin})");
                                            return false;
                                        }
                                        if (min > count)
                                        {
                                            this.setFailureMessage(tokens, $"Query result count is under min (got {count} min {min})");
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

                                    if (!String.IsNullOrEmpty(rawMax))
                                    {
                                        int max;
                                        if (!int.TryParse(rawMax, out max))
                                        {
                                            this.setFailureMessage(tokens, $"If max is included when comparison count, it must be a number ({rawMax})");
                                            return false;
                                        }
                                        if (max < count)
                                        {
                                            this.setFailureMessage(tokens, $"Query result count is over max (got {count} max {max})");
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
                                        this.setFailureMessage(tokens, $"Query result count out of range ({count})");
                                        return false;
                                    }
                                }
                            case "contains":
                            case "notcontains":
                                if (comparison == "contains" && queryValue.Matches.Count == 0)
                                {
                                    this.setFailureMessage(tokens, $"Query found no matches, so nothing to compare against");
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
                            default:
                                if(queryValue.Matches.Count == 0)
                                {
                                    this.setFailureMessage(tokens, $"Query found no matches, so nothing to compare against");
                                    return false;
                                }
                                string queryResult = queryValue.Matches[0].Value.GetString();
                                if (value.Equals(queryResult, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    if (matchcase && !value.Equals(queryResult))
                                    {
                                        //Invalid casing
                                        this.setFailureMessage(tokens, $"Value found but casing does not match (Result: {queryResult})");
                                        return false;
                                    }
                                    else
                                    {
                                        return true;
                                    }
                                }
                                else
                                {
                                    this.setFailureMessage(tokens, $"Queried Value does not match (Result: '{queryResult}', Expected: '{value}')");
                                    return false;
                                }
                        }
                    }
                }
                else
                {
                    //Invalid query
                    this.setFailureMessage(tokens, "Invalid Json Query");
                    return false;
                }
            }
            else
            {
                this.setFailureMessage(tokens, $"File not found at {path}");
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
