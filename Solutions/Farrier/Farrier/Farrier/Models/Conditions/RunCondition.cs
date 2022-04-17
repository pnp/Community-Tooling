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

        public override bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, string startingpath = "")
        {
            messages.Clear();
            var rName = tokens.DecodeString(rawRuleName);
            var result = runRule(rName, tokens.CleanTokens(), prefix+1, parentRule);
            if(!result.Succeeded)
            {
                this.setFailureMessage(tokens, $"Child rule \"{rName}\" failed");
            }
            messages.AddRange(result.messages);
            return result.Succeeded;
        }

        private string rawRuleName;
    }
}
