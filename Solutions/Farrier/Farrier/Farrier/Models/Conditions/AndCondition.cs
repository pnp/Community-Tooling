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

        public override bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, string startingpath = "")
        {
            messages.Clear();
            if (_subConditions.Count == 0)
            {
                return false;
            }

            bool success = true;

            foreach (var condition in _subConditions)
            {
                var result = condition.IsValid(tokens, runRule, parentRule, prefix+1, startingpath);
                childMessages.AddRange(condition.Messages);

                if (condition.IgnoreResult)
                    continue;

                if (!result)
                {
                    if(condition.IsWarning)
                    {
                        //Log as a warning, but don't fail the condition
                        childMessages.Add(new Message(MessageLevel.warning, condition.Name, tokens.DecodeString(condition.FailureMessage), prefix+1));
                    }
                    else
                    {
                        //no need to keep evaluating if even 1 sub is false
                        if (!condition.SuppressFailureMessage)
                        {
                            childMessages.Add(new Message(MessageLevel.error, condition.Name, tokens.DecodeString(condition.FailureMessage), prefix+1));
                        }
                        success = false;
                        break;
                    }
                }
            }

            LogChildMessages(success);
            return success;
        }

    }
}
