using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.IO;
using System.Text.RegularExpressions;

namespace Farrier.Models.Conditions
{
    class RegexMatchesCondition : BaseCondition
    {
        public new static RegexMatchesCondition FromNode(XmlNode conditionNode)
        {
            return new RegexMatchesCondition(conditionNode);
        }

        public RegexMatchesCondition(XmlNode conditionNode) : base(conditionNode)
        {
            rawPath = XmlHelper.XmlAttributeToString(conditionNode.Attributes["path"]);
            rawPattern = XmlHelper.XmlAttributeToString(conditionNode.Attributes["pattern"]);
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule = null, InspectionRule parentRule = null, int prefix = 0, string startingpath = "")
        {
            messages.Clear();
            var path = System.IO.Path.Combine(startingpath, tokens.DecodeString(rawPath));
            if (File.Exists(path))
            {
                var isNot = IsNot(tokens);
                var contents = File.ReadAllText(path);
                var pattern = tokens.DecodeString(rawPattern);

                //No explicit options are used, inline options are supported
                if (Regex.IsMatch(contents,pattern))
                {
                    //Match
                    if (!isNot)
                    {
                        return true;
                    }
                    else
                    {
                        //Flipped response
                        this.setFailureMessage(tokens, $"Pattern matched ({path})");
                        return false;
                    }
                }
                else
                {
                    //No Match

                    if(!isNot)
                    {
                        this.setFailureMessage(tokens, $"Pattern not matched ({path})");
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
                this.setFailureMessage(tokens, $"File not found at {path}");
                return false;
            }
        }

        private string rawPath;
        private string rawPattern;
    }
}
