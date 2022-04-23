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
                    case "log":
                        return LogCondition.FromNode(conditionNode);
                    case "if":
                        return IfCondition.FromNode(conditionNode);
                    case "foreachitem":
                        return ForEachItemCondition.FromNode(conditionNode);
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
                Name = type;
                OverriddenName = false;
            }
            else
            {
                OverriddenName = true;
            }

            this.IsWarning = XmlHelper.XmlAttributeToBool(conditionNode.Attributes["warn"]);
            this.failuremessage = XmlHelper.XmlAttributeToString(conditionNode.Attributes["failuremessage"]);
            ignoreResult = false;
            messages = new List<Message>();
        }

        public void Warn(TokenManager tokens, string text, int prefix)
        {
            messages.Add(new Message(MessageLevel.warning, Name, tokens.DecodeString(text), prefix));
        }

        public void Error(TokenManager tokens, string text, int prefix)
        {
            messages.Add(new Message(MessageLevel.error, Name, tokens.DecodeString(text), prefix));
        }

        public void Error(Exception ex, TokenManager tokens, string text, int prefix)
        {
            var msg = new Message(MessageLevel.error, Name, tokens.DecodeString(text), prefix);
            msg.Ex = ex;
            messages.Add(msg);
        }

        public void Info(TokenManager tokens, string text, int prefix)
        {
            messages.Add(new Message(MessageLevel.info, Name, tokens.DecodeString(text), prefix));
        }

        protected void setFailureMessage(TokenManager tokens, string text)
        {
            string message = text;
            if (!String.IsNullOrEmpty(this.failuremessage))
            {
                message = tokens.DecodeString(failuremessage);
            }
            this.failuremessage = message;
            this.suppressFailureMessage = false;
        }

        protected bool IsNot(TokenManager tokens)
        {
            return tokens.DecodeString(rawNot) == "true";
        }

        public abstract bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, string startingpath = "");

        public readonly string type;
        public readonly bool IsWarning;
        protected string failuremessage;
        public string FailureMessage { get { return String.IsNullOrEmpty(this.failuremessage) ? $"Unspecified {(this.IsWarning ? "warning" : "error")} from {this.type} condition" : this.failuremessage; } }
        protected bool suppressFailureMessage;
        public bool SuppressFailureMessage { get { return suppressFailureMessage; } }

        protected List<Message> messages;
        public List<Message> Messages { get { return messages; } }

        private string rawNot;
        public string Name;
        public bool OverriddenName;

        protected bool ignoreResult;
        public bool IgnoreResult { get { return ignoreResult; } }
    }
}
