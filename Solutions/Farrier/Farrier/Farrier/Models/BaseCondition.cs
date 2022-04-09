using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;

namespace Farrier.Models
{
    abstract class BaseCondition
    {
        public static BaseCondition FromNode(XmlNode conditionNode)
        {
            if(conditionNode != null && conditionNode.NodeType == XmlNodeType.Element)
            {
                var conditionType = conditionNode.LocalName.ToLower();
                switch (conditionType)
                {
                    case "and":
                        return AndCondition.FromNode(conditionNode);
                    case "fileexists":
                        return FileExistsCondition.FromNode(conditionNode);
                    case "folderexists":
                        return FolderExistsCondition.FromNode(conditionNode);
                    case "run":
                        return RunCondition.FromNode(conditionNode);
                    default:
                        return null;
                }
            }
            return null;
        }

        protected BaseCondition(XmlNode conditionNode)
        {
            this.type = conditionNode.LocalName.ToLower();

            this.IsWarning = XmlHelper.XmlAttributeToBool(conditionNode.Attributes["warn"]);
            this.failuremessage = XmlHelper.XmlAttributeToString(conditionNode.Attributes["failuremessage"]);
            this.warnings = new List<string>();
            this.errors = new List<string>();
        }

        public abstract bool IsValid(TokenManager tokens, DelRunRule runRule, int prefix = 0, string startingpath = "");

        public readonly string type;
        public readonly bool IsWarning;
        protected string failuremessage;
        public string FailureMessage { get { return String.IsNullOrEmpty(this.failuremessage) ? $"Unspecified {(this.IsWarning ? "warning" : "error")} from {this.type} condition" : this.failuremessage; } }
        protected List<string> warnings;
        protected List<string> errors;
        public List<string> Warnings { get { return this.warnings; } }
        public List<string> Errors { get { return this.errors; } }
    }
}
