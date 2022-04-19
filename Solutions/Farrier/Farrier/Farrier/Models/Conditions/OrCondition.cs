using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.Linq;

namespace Farrier.Models.Conditions
{
    class OrCondition : BaseParentCondition
    {
        public new static OrCondition FromNode(XmlNode conditionNode)
        {
            return new OrCondition(conditionNode);
        }

        public OrCondition(XmlNode conditionNode) : base(conditionNode)
        {

        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, string startingpath = "")
        {
            messages.Clear();
            if (_subConditions.Count == 0)
            {
                return false;
            }

            int skip = ValidateSkip(tokens, "conditions", prefix);
            int limit = ValidateLimit(tokens, "conditions", prefix);

            bool success = false;

            var conditions = _subConditions;
            if (skip > 0)
                conditions = conditions.Skip(skip).ToList();
            if (limit > 0 && conditions.Count > limit)
                conditions = conditions.SkipLast(conditions.Count - limit).ToList();

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
                        if(!condition.SuppressFailureMessage)
                        {
                            childMessages.Add(new Message(MessageLevel.error, condition.Name, tokens.DecodeString(condition.FailureMessage), prefix+1));
                        }
                    }
                }
                else
                {
                    success = true;
                    break;
                }
            }

            LogChildMessages(success);
            return success;
        }

    }
}
