using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.Linq;

namespace Farrier.Models.Conditions
{
    class LogCondition : BaseCondition
    {
        public new static LogCondition FromNode(XmlNode conditionNode)
        {
            return new LogCondition(conditionNode);
        }

        public LogCondition(XmlNode conditionNode) : base(conditionNode)
        {
            rawText = XmlHelper.XmlAttributeToString(conditionNode.Attributes["text"]);
            ignoreResult = true;
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, string startingpath = "")
        {
            messages.Clear();
            Info(tokens, rawText, prefix);
            return true;
        }

        private string rawText;
    }
}
