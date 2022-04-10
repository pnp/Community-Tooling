using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.IO;

namespace Farrier.Models.Conditions
{
    class RunCondition : BaseCondition
    {
        public new static RunCondition FromNode(XmlNode conditionNode)
        {
            return new RunCondition(conditionNode);
        }

        public RunCondition(XmlNode conditionNode) : base(conditionNode)
        {
            rawRuleName = XmlHelper.XmlAttributeToString(conditionNode.Attributes["rule"]);
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, int messagePrefix = 0, string startingpath = "")
        {
            this.messages = new List<Message>();
            var rName = tokens.DecodeString(rawRuleName);
            var result = runRule(rName, tokens.CleanTokens(), prefix+1, parentRule);
            if(!result)
            {
                if (String.IsNullOrEmpty(this.failuremessage))
                {
                    this.failuremessage = $"Child rule \"{rName}\" failed";
                }
                else
                {
                    this.failuremessage = tokens.DecodeString(failuremessage);
                }
            }
            return result;
        }

        private string rawRuleName;
    }
}
