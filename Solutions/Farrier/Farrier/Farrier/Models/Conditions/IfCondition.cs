using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.Linq;

namespace Farrier.Models.Conditions
{
    class IfCondition : BaseParentCondition
    {
        public new static IfCondition FromNode(XmlNode conditionNode)
        {
            return new IfCondition(conditionNode);
        }

        public IfCondition(XmlNode conditionNode) : base(conditionNode)
        {

        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, string startingpath = "")
        {
            messages.Clear();
            if(_subConditions.Count < 2)
            {
                messages.Add(new Message(MessageLevel.warning, Name, $"if conditions require at least 2 sub conditions (found {_subConditions.Count}), 1=condition, 2=when true, 3=when false (optional)", prefix));
                ignoreResult = true;
                return true;
            }
            if(_subConditions.Count > 3)
            {
                messages.Add(new Message(MessageLevel.warning, Name, $"if conditions can have a maximum of 3 sub conditions (found {_subConditions.Count}), 1=condition, 2=when true, 3=when false (optional)", prefix));
                ignoreResult = true;
                return true;
            }

            var ifresult = _subConditions[0].IsValid(tokens, runRule, parentRule, prefix + 1, startingpath);
            //Failure Messages are purposely not bubbled up since it's an expected thing
            messages.AddRange(_subConditions[0].Messages);
            if (_subConditions[0].IgnoreResult)
                ifresult = true;

            if(!ifresult && _subConditions.Count == 2)
            {
                //No false condition, so just be done
                return true;
            }
            var childCondition = ifresult ? _subConditions[1] : _subConditions[2];
            bool success = childCondition.IsValid(tokens, runRule, parentRule, prefix + 1, startingpath);
            messages.AddRange(childCondition.Messages);
            if (childCondition.IgnoreResult)
                success = true;
            
            if(!success)
            {
                if(childCondition.IsWarning)
                {
                    messages.Add(new Message(MessageLevel.warning, childCondition.Name, tokens.DecodeString(childCondition.FailureMessage), prefix + 1));
                }
                else
                {
                    if (!childCondition.SuppressFailureMessage)
                    {
                        messages.Add(new Message(MessageLevel.error, childCondition.Name, tokens.DecodeString(childCondition.FailureMessage), prefix + 1));
                    }
                }
            }

            return success;
        }
    }
}
