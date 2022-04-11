using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.Linq;

namespace Farrier.Models.Conditions
{
    class AndCondition : BaseParentCondition
    {
        public new static AndCondition FromNode(XmlNode conditionNode)
        {
            return new AndCondition(conditionNode);
        }

        public AndCondition(XmlNode conditionNode) : base(conditionNode)
        {
            
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, int messagePrefix = 0, string startingpath = "")
        {
            this.messages = new List<Message>();
            if (_subConditions.Count == 0)
            {
                return false;
            }

            var subMessages = new List<Message>();

            foreach (var condition in _subConditions)
            {
                var result = condition.IsValid(tokens, runRule, parentRule, prefix+1, messagePrefix+1, startingpath);

                //bubble up any messages
                subMessages.AddRange(condition.Messages);

                if (!result)
                {
                    if(condition.IsWarning)
                    {
                        //Add its warning, but don't fail the condition
                        subMessages.Add(Message.Warning(tokens.DecodeString(condition.FailureMessage)));
                    }
                    else
                    {
                        //no need to keep evaluating if even 1 sub is false
                        if (!condition.SuppressFailureMessage)
                        {
                            subMessages.Add(Message.Error(tokens.DecodeString(condition.FailureMessage)));
                        }
                        this.messages.AddRange(subMessages); //Only bubble up all in failure situations
                        return false;
                    }
                }
            }
            this.messages.AddRange(subMessages.Where(m => m.Level != MessageLevel.error));
            return true;
        }

    }
}
