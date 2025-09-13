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
                    case "regexmatches":
                        return RegexMatchesCondition.FromNode(conditionNode);
                    default:
                        return null;
                }
            }
            return null;
        }

        protected BaseCondition(XmlNode conditionNode)
        {
            type = conditionNode.LocalName.ToLower();

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

            IsWarning = XmlHelper.XmlAttributeToBool(conditionNode.Attributes["warn"]);
            failuremessageTemplate = XmlHelper.XmlAttributeToString(conditionNode.Attributes["failuremessage"]);
            ignoreResult = false;
            messages = new List<Message>();
            propertyMap = new Dictionary<string, string>();
        }

        public void Warn(string text, int prefix)
        {
            messages.Add(new Message(MessageLevel.warning, Name, text, prefix));
        }

        public void Warn(TokenManager tokens, string text, int prefix)
        {
            messages.Add(new Message(MessageLevel.warning, Name, tokens.DecodeString(text), prefix));
        }

        public void Error(string text, int prefix)
        {
            messages.Add(new Message(MessageLevel.error, Name, text, prefix));
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

        public void Info(string text, int prefix)
        {
            messages.Add(new Message(MessageLevel.info, Name, text, prefix));
        }

        public void Info(TokenManager tokens, string text, int prefix)
        {
            messages.Add(new Message(MessageLevel.info, Name, tokens.DecodeString(text), prefix));
        }

        protected void setFailureMessage(TokenManager tokens, string text, List<Suppression> potentialSuppressions)
        {
            string message = text;
            if (!string.IsNullOrEmpty(failuremessageTemplate))
            {
                message = tokens.DecodeString(failuremessageTemplate);
            }
            if(!isSuppressed(message, potentialSuppressions, tokens))
            {
                failuremessage = message;
                suppressFailureMessage = false;
            } else
            {
                failuremessage = string.Empty;
                suppressFailureMessage = true;
            }
        }

        protected bool IsNot(TokenManager tokens)
        {
            return tokens.DecodeString(rawNot) == "true";
        }

        protected bool isSuppressed(string message, List<Suppression> potentialSuppressions, TokenManager tokens)
        {
            foreach (var suppression in potentialSuppressions)
            {
                if (suppression.IsSuppressed(propertyMap, tokens, message))
                {
                    // Suppressed, no need to look at the rest
                    return true;
                }
            }
            // Made it this far, so not suppressed
            return false;
        }

        public abstract bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, string startingpath = "");

        public readonly string type;
        public readonly bool IsWarning;
        protected string failuremessageTemplate;
        protected string failuremessage;
        public string FailureMessage { get { return string.IsNullOrEmpty(failuremessage) ? $"Unspecified {(IsWarning ? "warning" : "error")} from {type} condition" : failuremessage; } }
        protected bool suppressFailureMessage;
        public bool SuppressFailureMessage { get { return suppressFailureMessage; } }

        protected List<Message> messages;
        public List<Message> Messages { get { return messages; } }

        private string rawNot;
        public string Name;
        public bool OverriddenName;

        protected bool ignoreResult;
        public bool IgnoreResult { get { return ignoreResult; } }

        protected Dictionary<string, string> propertyMap;
    }
}
