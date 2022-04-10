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
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule = null, InspectionRule parentRule = null, int prefix = 0, int messagePrefix = 0, string startingpath = "")
        {
            this.messages = new List<Message>();
            var comparison = tokens.DecodeString(rawComparison);
            if (comparison != "equals" && comparison != "count")
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
                                return false;
                            default:
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
                                    this.setFailureMessage(tokens, $"Queried Value does not match (Result: {queryResult})");
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
    }
}
