using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.IO;

namespace Farrier.Models.Conditions
{
    class FileContainsCondition : BaseCondition
    {
        public new static FileContainsCondition FromNode(XmlNode conditionNode)
        {
            return new FileContainsCondition(conditionNode);
        }

        public FileContainsCondition(XmlNode conditionNode) : base(conditionNode)
        {
            rawPath = XmlHelper.XmlAttributeToString(conditionNode.Attributes["path"]);
            rawMatchCase = XmlHelper.XmlAttributeToString(conditionNode.Attributes["matchcase"]);
            rawText = XmlHelper.XmlAttributeToString(conditionNode.Attributes["text"]);
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule = null, InspectionRule parentRule = null, int prefix = 0, int messagePrefix = 0, string startingpath = "")
        {
            this.messages = new List<Message>();
            var path = System.IO.Path.Combine(startingpath, tokens.DecodeString(rawPath));
            if (File.Exists(path))
            {
                var contents = File.ReadAllText(path);
                var searchText = tokens.DecodeString(rawText);
                var matchcase = tokens.DecodeString(rawMatchCase) == "true";
                if (contents.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (matchcase && !contents.Contains(searchText))
                    {
                        //Invalid casing
                        this.setFailureMessage(tokens, $"Text exists but casing does not match ({path})");
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    this.setFailureMessage(tokens, $"Specified text not found in {path}");
                    return false;
                }
            }
            else
            {
                this.setFailureMessage(tokens, $"File not found at {path}");
                return false;
            }
        }

        private string rawPath;
        private string rawMatchCase;
        private string rawText;
    }
}
