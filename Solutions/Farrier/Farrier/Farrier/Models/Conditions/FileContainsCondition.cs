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
                var isNot = IsNot(tokens);
                var contents = File.ReadAllText(path);
                var searchText = tokens.DecodeString(rawText);
                var matchcase = tokens.DecodeString(rawMatchCase) == "true";
                if (contents.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                {
                    //Text is in the file (regardless of casing)

                    if (matchcase && !contents.Contains(searchText))
                    {
                        //Invalid casing
                        if (!isNot)
                        {
                            this.setFailureMessage(tokens, $"Text exists but casing does not match ({path})");
                            return false;
                        }
                        else
                        {
                            //Flipped response
                            this.setSuccessMessage(tokens, $"Text exists but casing does not match ({path})");
                            return true;
                        }
                    }
                    else
                    {
                        if(!isNot)
                        {
                            this.setSuccessMessage(tokens, "Text found");
                            return true;
                        }
                        else
                        {
                            //Flipped response
                            this.setFailureMessage(tokens, "Text found");
                            return false;
                        }
                    }
                }
                else
                {
                    //Text is NOT in the file (regardless of casing)

                    if(!isNot)
                    {
                        this.setFailureMessage(tokens, $"Specified text not found in {path}");
                        return false;
                    }
                    else
                    {
                        //Flipped response
                        this.setSuccessMessage(tokens, $"Specified text not found in {path}");
                        return true;
                    }
                    
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
