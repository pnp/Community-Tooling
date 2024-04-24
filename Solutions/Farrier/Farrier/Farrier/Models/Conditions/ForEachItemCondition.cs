using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.IO;
using System.Linq;

namespace Farrier.Models.Conditions
{
    class ForEachItemCondition : BaseParentCondition
    {
        public new static ForEachItemCondition FromNode(XmlNode conditionNode)
        {
            return new ForEachItemCondition(conditionNode);
        }

        public ForEachItemCondition(XmlNode conditionNode) : base(conditionNode)
        {
            rawItems = XmlHelper.XmlAttributeToString(conditionNode.Attributes["items"]);
            rawSeparator = XmlHelper.XmlAttributeToString(conditionNode.Attributes["separator"]);
            if(String.IsNullOrEmpty(rawSeparator))
                rawSeparator = ",";

            rawTransform = XmlHelper.XmlAttributeToString(conditionNode.Attributes["transform"]);
            if (String.IsNullOrEmpty(rawTransform))
                rawTransform = "@@currentvalue@@";
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, string startingpath = "")
        {
            messages.Clear();
            propertyMap.Clear();
            var potentialSuppressions = parentRule.GetSuppressionsForCondition(this);

            if (_subConditions.Count == 0)
            {
                return false;
            }

            int skip = ValidateSkip(tokens, "items", prefix);
            int limit = ValidateLimit(tokens, "items", prefix);
            var quiet = tokens.DecodeString(rawQuiet) == "true";

            var itemstring = tokens.DecodeString(rawItems);
            var separator = tokens.DecodeString(rawSeparator);

            var items = itemstring.Split(separator);

            

            bool success = true;
            if (skip > 0)
                items = items.Skip(skip).ToArray();
            if (limit > 0 && items.Length > limit)
                items = items.SkipLast(items.Length - limit).ToArray();

            var currentitem = 1;
            var totalitems = items.Length;
            foreach (var item in items)
            {
                var transformedItem = tokens.DecodeString(rawTransform.Replace("@@currentvalue@@", item));
                if(!quiet)
                    childMessages.Add(new Message(MessageLevel.info, Name, $"Item ({currentitem}/{totalitems}): {transformedItem}", prefix));

                var foreachTokens = new TokenManager(tokens);
                foreachTokens.NestToken("Each", transformedItem);
                foreachTokens.NestToken("TotalItems", totalitems.ToString());

                foreach (var condition in _subConditions)
                {
                    var result = condition.IsValid(foreachTokens, runRule, parentRule, prefix+1, startingpath);
                    childMessages.AddRange(condition.Messages);

                    if (condition.IgnoreResult)
                        continue;

                    if (!result)
                    {
                        if (condition.IsWarning)
                        {
                            //Log as warning, but don't fail the condition
                            childMessages.Add(new Message(MessageLevel.warning, condition.Name, tokens.DecodeString(condition.FailureMessage), prefix + 1));
                        }
                        else
                        {
                            //no need to keep evaluating if even 1 sub is false
                            if(!condition.SuppressFailureMessage)
                            {
                                childMessages.Add(new Message(MessageLevel.error, condition.Name, tokens.DecodeString(condition.FailureMessage), prefix + 1));
                            }
                            this.setFailureMessage(tokens, $"Sub Condition failure during processing of item {transformedItem}", potentialSuppressions);
                            success = false;
                            break;
                        }
                    }
                }
                if (!success)
                    break;
                currentitem += 1;
            }
            LogChildMessages(success);
            return success;
        }

        private readonly string rawItems;
        private readonly string rawSeparator;
        private readonly string rawTransform;
    }
}
