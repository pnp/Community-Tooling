﻿using System;
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

            bool success = false;

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
                        childMessages.Add(new Message(MessageLevel.warning, condition.Name, condition.FailureMessage, prefix+1));
                    }
                    else
                    {
                        if(!condition.SuppressFailureMessage)
                        {
                            childMessages.Add(new Message(MessageLevel.error, condition.Name, condition.FailureMessage, prefix+1));
                        }
                    }
                }
                else
                {
                    success = true;
                    break;
                }
            }

            LogChildMessages(tokens, prefix, success);
            return success;
        }

    }
}
