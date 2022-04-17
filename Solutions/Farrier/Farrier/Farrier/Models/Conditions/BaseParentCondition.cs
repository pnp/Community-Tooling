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
        }

        protected List<Message> childMessages;

        protected void LogChildMessages(TokenManager tokens, int prefix, bool skipErrors = false)
        {
            foreach(var message in childMessages)
            {
                if (message.Level != MessageLevel.error || (message.Level == MessageLevel.error && !skipErrors))
                    messages.Add(message);
            }
        }

    }
}
