using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;

namespace Farrier.Models
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

        public override bool IsValid(TokenManager tokens, string prefix = "", string startingpath = "")
        {

            if(_subConditions.Count == 0)
            {
                return false;
            }

            foreach (var condition in _subConditions)
            {
                var result = condition.IsValid(tokens, prefix, startingpath);

                //bubble up any warnings or errors
                this.warnings.AddRange(condition.Warnings);
                this.errors.AddRange(condition.Errors);

                if (!result)
                {
                    if(condition.IsWarning)
                    {
                        //Add its warning, but don't fail the condition
                        warnings.Add(tokens.DecodeString(condition.FailureMessage));
                    }
                    else
                    {
                        //no need to keep evaluating if even 1 sub is false
                        errors.Add(tokens.DecodeString(condition.FailureMessage));
                        return false;
                    }
                }
            }
            return true;
        }

    }
}
