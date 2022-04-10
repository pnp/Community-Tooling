using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;

namespace Farrier.Models.Conditions
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
                    case "foreachfolder":
                        return ForEachFolderCondition.FromNode(conditionNode);
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
            this.successmessage = XmlHelper.XmlAttributeToString(conditionNode.Attributes["successmessage"]);
            this.messages = new List<Message>();
        }

        public abstract bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, int messagePrefix = 0, string startingpath = "");

        public readonly string type;
        public readonly bool IsWarning;
        protected string successmessage;
        protected string failuremessage;
        public string FailureMessage { get { return String.IsNullOrEmpty(this.failuremessage) ? $"Unspecified {(this.IsWarning ? "warning" : "error")} from {this.type} condition" : this.failuremessage; } }
        public string SuccessMessage { get { return String.IsNullOrEmpty(this.failuremessage) ? $"{this.type} condition succeeded" : this.successmessage; } }
        protected List<Message> messages;
        public List<Message> Messages { get { return this.messages; } }
    }
}
