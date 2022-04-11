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
            this.suppressFailureMessage = String.IsNullOrEmpty(failuremessage);
        }

    }
}
