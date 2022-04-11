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

        public override bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, int messagePrefix = 0, string startingpath = "")
        {
            bool when = tokens.DecodeString(rawWhen) == "true";
            if(when)
            {
                return base.IsValid(tokens, runRule, parentRule, prefix, messagePrefix, startingpath);
            }
            else
            {
                this.setSuccessMessage(tokens, "OnlyWhen skipped (condition was false)");
                return true;
            }
        }

        private string rawWhen;
    }
}
