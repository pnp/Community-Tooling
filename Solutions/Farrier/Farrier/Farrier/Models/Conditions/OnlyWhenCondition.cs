using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.Linq;

namespace Farrier.Models.Conditions
{
    class OnlyWhenCondition : AndCondition
    {
        public new static OnlyWhenCondition FromNode(XmlNode conditionNode)
        {
            return new OnlyWhenCondition(conditionNode);
        }

        public OnlyWhenCondition(XmlNode conditionNode) : base(conditionNode)
        {
            rawWhen = XmlHelper.XmlAttributeToString(conditionNode.Attributes["when"]);
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, string startingpath = "")
        {
            messages.Clear();
            string decodedWhen = tokens.DecodeString(rawWhen);
            if(decodedWhen != "true" && decodedWhen != "false")
            {
                messages.Add(new Message(MessageLevel.warning, Name, $"when not evaluating correctly, skipping sub conditions. Expected 'true' or 'false' but got '{decodedWhen}'"));
                return true;
            }
            bool when = decodedWhen == "true";
            if(when)
            {
                return base.IsValid(tokens, runRule, parentRule, prefix, startingpath);
            }
            else
            {
                return true;
            }
        }

        private string rawWhen;
    }
}
