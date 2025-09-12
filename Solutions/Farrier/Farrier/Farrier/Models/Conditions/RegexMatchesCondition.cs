using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

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
            propertyMap.Clear();
            var potentialSuppressions = parentRule.GetSuppressionsForCondition(this);

            var path = Path.Combine(startingpath, tokens.DecodeString(rawPath));
            propertyMap.Add("path", path);
            if (File.Exists(path))
            {
                var isNot = IsNot(tokens);
                var contents = File.ReadAllText(path).Replace("\r\n", "\n").Replace('\r', '\n');
                var pattern = tokens.DecodeString(rawPattern);

                //No explicit options are used, inline options are supported
                if (Regex.IsMatch(contents,pattern))
                {
                    var match = Regex.Match(contents, pattern);
                    int lineNumber = contents.Substring(0, match.Index).Count(c => c == '\n') + 1;
                    tokens.NestToken("LineNumber", lineNumber.ToString());
                    //Match
                    if (!isNot)
                    {
                        return true;
                    }
                    else
                    {
                        //Flipped response
                        setFailureMessage(tokens, $"Pattern matched ({path})", potentialSuppressions);
                        return false;
                    }
                }
                else
                {
                    //No Match

                    if(!isNot)
                    {
                        setFailureMessage(tokens, $"Pattern not matched ({path})", potentialSuppressions);
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
                setFailureMessage(tokens, $"File not found at {path}", potentialSuppressions);
                return false;
            }
        }

        private string rawPath;
        private string rawPattern;
    }
}
