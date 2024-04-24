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

        public override bool IsValid(TokenManager tokens, DelRunRule runRule = null, InspectionRule parentRule = null, int prefix = 0, string startingpath = "")
        {
            messages.Clear();
            propertyMap.Clear();
            var potentialSuppressions = parentRule.GetSuppressionsForCondition(this);

            var path = System.IO.Path.Combine(startingpath, tokens.DecodeString(rawPath));
            propertyMap.Add("path", path);


            if (File.Exists(path))
            {
                var isNot = IsNot(tokens);
                var contents = File.ReadAllText(path).Replace("\r\n", "\n").Replace('\r', '\n');
                var searchText = tokens.DecodeString(rawText).Replace("\\r", "\r").Replace("\\n","\n");
                var matchcase = tokens.DecodeString(rawMatchCase) == "true";
                if (contents.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                {
                    //Text is in the file (regardless of casing)

                    if (matchcase && !contents.Contains(searchText))
                    {
                        //Invalid casing
                        if (!isNot)
                        {
                            this.setFailureMessage(tokens, $"Text exists but casing does not match ({path})", potentialSuppressions);
                            return false;
                        }
                        else
                        {
                            //Flipped response
                            return true;
                        }
                    }
                    else
                    {
                        if(!isNot)
                        {
                            return true;
                        }
                        else
                        {
                            //Flipped response
                            this.setFailureMessage(tokens, "Text found", potentialSuppressions);
                            return false;
                        }
                    }
                }
                else
                {
                    //Text is NOT in the file (regardless of casing)

                    if(!isNot)
                    {
                        this.setFailureMessage(tokens, $"Specified text not found in {path}", potentialSuppressions);
                        return false;
                    }
                    else
                    {
                        //Flipped response
                        return true;
                    }
                    
                }
            }
            else
            {
                this.setFailureMessage(tokens, $"File not found at {path}", potentialSuppressions);
                return false;
            }
        }

        private string rawPath;
        private string rawMatchCase;
        private string rawText;
    }
}
