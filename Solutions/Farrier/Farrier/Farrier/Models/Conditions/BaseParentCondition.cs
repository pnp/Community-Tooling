using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;

namespace Farrier.Models.Conditions
{
    abstract class BaseParentCondition : BaseCondition
    {
        protected List<BaseCondition> _subConditions;

        public BaseParentCondition(XmlNode conditionNode) : base(conditionNode)
        {
            _subConditions = new List<BaseCondition>();
            //int index = 1;
            foreach (XmlNode child in conditionNode.ChildNodes)
            {
                var childCondition = BaseCondition.FromNode(child);
                if(childCondition != null)
                {
                    /*if(!childCondition.OverriddenName && index > 1)
                    {
                        childCondition.Name = $"{childCondition.type}.{index}";
                    }*/
                    _subConditions.Add(childCondition);
                    //index += 1;
                }
            }
            if(_subConditions.Count == 0 && String.IsNullOrEmpty(this.failuremessage))
            {
                this.failuremessage = "No valid child conditions!";
            }
            this.suppressFailureMessage = String.IsNullOrEmpty(failuremessage);
            this.childMessages = new List<Message>();
            rawSkip = XmlHelper.XmlAttributeToString(conditionNode.Attributes["skip"]);
            rawLimit = XmlHelper.XmlAttributeToString(conditionNode.Attributes["limit"]);
        }

        protected int ValidateSkip(TokenManager tokens, string itemsName, int prefix)
        {
            string decodedSkip = tokens.DecodeString(rawSkip);
            int skip = 0;
            if (!String.IsNullOrEmpty(decodedSkip) && !int.TryParse(decodedSkip, out skip))
            {
                messages.Add(new Message(MessageLevel.warning, Name, $"skip value is invalid. Expected a number but got '{decodedSkip}', no {itemsName} will be skipped", prefix));
            }
            if (skip > 0)
                messages.Add(new Message(MessageLevel.info, Name, $"Skipping the first {skip} {itemsName}", prefix));

            return skip;
        }

        protected int ValidateLimit(TokenManager tokens, string itemsName, int prefix)
        {
            string decodedLimit = tokens.DecodeString(rawLimit);
            int limit = 0;
            if (!String.IsNullOrEmpty(decodedLimit) && !int.TryParse(decodedLimit, out limit))
            {
                messages.Add(new Message(MessageLevel.warning, Name, $"limit value is invalid. Expected a number but got '{decodedLimit}', no limit of {itemsName} will be enforced", prefix));
            }
            if (limit > 0)
                messages.Add(new Message(MessageLevel.info, Name, $"Limiting to the first {limit} {itemsName}", prefix));

            return limit;
        }

        protected List<Message> childMessages;

        protected void LogChildMessages(bool skipErrors = false)
        {
            foreach(var message in childMessages)
            {
                if (message.Level != MessageLevel.error || (message.Level == MessageLevel.error && !skipErrors))
                    messages.Add(message);
            }
        }

        protected string rawSkip;
        protected string rawLimit;

    }
}
