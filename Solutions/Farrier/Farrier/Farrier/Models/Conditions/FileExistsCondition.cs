using System;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.IO;
using System.Linq;

namespace Farrier.Models.Conditions
{
    class FileExistsCondition : BaseCondition
    {
        public new static FileExistsCondition FromNode(XmlNode conditionNode)
        {
            return new FileExistsCondition(conditionNode);
        }

        public FileExistsCondition(XmlNode conditionNode) : base(conditionNode)
        {
            rawPath = XmlHelper.XmlAttributeToString(conditionNode.Attributes["path"]);
            rawMatchCase = XmlHelper.XmlAttributeToString(conditionNode.Attributes["matchcase"]);
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule = null, InspectionRule parentRule = null, int prefix = 0, string startingpath = "")
        {
            messages.Clear();
            propertyMap.Clear();
            var potentialSuppressions = parentRule.GetSuppressionsForCondition(this);

            var path = PathNormalizer.Normalize(Path.Combine(startingpath, tokens.DecodeString(rawPath)));
            propertyMap.Add("path", path);
            var matchcase = tokens.DecodeString(rawMatchCase) == "true";
            if (File.Exists(path))
            {
                if(matchcase)
                {
                    var origFilename = Path.GetFileName(path);
                    var files = Directory.GetFiles(Path.GetDirectoryName(path)).OrderBy(f => Path.GetFileName(f)).ToArray();
                    foreach (var file in files)
                    {
                        var filename = Path.GetFileName(file);
                        if(origFilename.Equals(filename,StringComparison.CurrentCultureIgnoreCase) && !origFilename.Equals(filename))
                        {
                            //Invalid casing
                            setFailureMessage(tokens, $"File exists but casing does not match (found {filename})", potentialSuppressions);
                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                setFailureMessage(tokens, $"File not found at {path}", potentialSuppressions);
                return false;
            }
        }

        private string rawPath;
        public string rawMatchCase;
    }
}
