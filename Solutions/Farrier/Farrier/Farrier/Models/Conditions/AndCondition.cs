using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;

namespace Farrier.Models.Conditions
{
    class AndCondition : BaseCondition
    {
        private List<BaseCondition> _subConditions;
        public new static AndCondition FromNode(XmlNode conditionNode)
        {
            return new AndCondition(conditionNode);
        }

        public AndCondition(XmlNode conditionNode) : base(conditionNode)
        {
            _subConditions = new List<BaseCondition>();
            foreach (XmlNode child in conditionNode.ChildNodes)
            {
                var childCondition = BaseCondition.FromNode(child);
                if(childCondition != null)
                {
                    _subConditions.Add(childCondition);
                }
            }
            if(_subConditions.Count == 0 && String.IsNullOrEmpty(this.failuremessage))
            {
                this.failuremessage = "No valid child conditions!";
            }
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, int messagePrefix = 0, string startingpath = "")
        {
            this.messages = new List<Message>();
            if (_subConditions.Count == 0)
            {
                return false;
            }

            foreach (var condition in _subConditions)
            {
                var result = condition.IsValid(tokens, runRule, parentRule, prefix+1, messagePrefix+1, startingpath);

                //bubble up any messages
                this.messages.AddRange(condition.Messages);

                if (!result)
                {
                    if(condition.IsWarning)
                    {
                        //Add its warning, but don't fail the condition
                        messages.Add(Message.Warning(tokens.DecodeString(condition.FailureMessage)));
                    }
                    else
                    {
                        //no need to keep evaluating if even 1 sub is false
                        messages.Add(Message.Error(tokens.DecodeString(condition.FailureMessage)));
                        return false;
                    }
                }
            }
            return true;
        }

    }
}
