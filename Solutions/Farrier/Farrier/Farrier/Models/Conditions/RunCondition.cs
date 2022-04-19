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
            rawInput = XmlHelper.XmlAttributeToString(conditionNode.Attributes["input"]);
            rawQuiet = XmlHelper.XmlAttributeToString(conditionNode.Attributes["quiet"]);

            //TODO: Add support for passing full tokens to children
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, string startingpath = "")
        {
            messages.Clear();
            var rName = tokens.DecodeString(rawRuleName);
            var input = tokens.DecodeString(rawInput);
            var quiet = tokens.DecodeString(rawQuiet) == "true";
            var runTokens = new TokenManager(tokens);
            runTokens.NestToken("Input", input);

            var result = runRule(rName, runTokens.CleanTokens(), prefix+1, parentRule, quiet);
            if(!result.Succeeded)
            {
                this.setFailureMessage(tokens, $"Child rule \"{rName}\" failed");
            }
            messages.AddRange(result.messages);
            return result.Succeeded;
        }

        private string rawRuleName;
        private string rawInput;
        private string rawQuiet;
    }
}
