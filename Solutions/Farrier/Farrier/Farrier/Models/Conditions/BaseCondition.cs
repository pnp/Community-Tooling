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
                    case "or":
                        return OrCondition.FromNode(conditionNode);
                    case "fileexists":
                        return FileExistsCondition.FromNode(conditionNode);
                    case "folderexists":
                        return FolderExistsCondition.FromNode(conditionNode);
                    case "run":
                        return RunCondition.FromNode(conditionNode);
                    case "foreachfolder":
                        return ForEachFolderCondition.FromNode(conditionNode);
                    case "foreachfile":
                        return ForEachFileCondition.FromNode(conditionNode);
                    case "filecontains":
                        return FileContainsCondition.FromNode(conditionNode);
                    case "jsonquery":
                        return JsonQueryCondition.FromNode(conditionNode);
                    case "only":
                        return OnlyWhenCondition.FromNode(conditionNode);
                    default:
                        return null;
                }
            }
            return null;
        }

        protected BaseCondition(XmlNode conditionNode)
        {
            this.type = conditionNode.LocalName.ToLower();

            rawNot = XmlHelper.XmlAttributeToString(conditionNode.Attributes["not"]);
            Name = XmlHelper.XmlAttributeToString(conditionNode.Attributes["name"]);
            if(string.IsNullOrEmpty(Name))
            {
                Name = $"[{type}]";
                OverriddenName = false;
            }
            else
            {
                OverriddenName = true;
            }

            this.IsWarning = XmlHelper.XmlAttributeToBool(conditionNode.Attributes["warn"]);
            this.failuremessage = XmlHelper.XmlAttributeToString(conditionNode.Attributes["failuremessage"]);
            this.successmessage = XmlHelper.XmlAttributeToString(conditionNode.Attributes["successmessage"]);
            this.messages = new List<Message>();
        }

        protected void setFailureMessage(TokenManager tokens, string text)
        {
            string message = text;
            if (!String.IsNullOrEmpty(this.failuremessage))
            {
                message = tokens.DecodeString(failuremessage);
            }
            this.failuremessage = $"{Name}: {message}";
            this.suppressFailureMessage = false;
        }

        protected void setSuccessMessage(TokenManager tokens, string text)
        {
            string message = text;
            if (!String.IsNullOrEmpty(this.successmessage))
            {
                message = tokens.DecodeString(successmessage);
            }
            this.successmessage = $"{Name}: {message}";
        }

        protected bool IsNot(TokenManager tokens)
        {
            return tokens.DecodeString(rawNot) == "true";
        }

        public abstract bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, int messagePrefix = 0, string startingpath = "");

        public readonly string type;
        public readonly bool IsWarning;
        protected string successmessage;
        protected string failuremessage;
        public string FailureMessage { get { return String.IsNullOrEmpty(this.failuremessage) ? $"Unspecified {(this.IsWarning ? "warning" : "error")} from {this.type} condition" : this.failuremessage; } }
        protected bool suppressFailureMessage;
        public bool SuppressFailureMessage { get { return suppressFailureMessage; } }
        public string SuccessMessage { get { return String.IsNullOrEmpty(this.failuremessage) ? $"{this.type} condition succeeded" : this.successmessage; } }
        protected List<Message> messages;
        public List<Message> Messages { get { return this.messages; } }

        private string rawNot;
        public string Name;
        public bool OverriddenName;
    }
}
