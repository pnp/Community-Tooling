using System;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.IO;
using System.Linq;

namespace Farrier.Models.Conditions
{
    class FolderExistsCondition : BaseCondition
    {
        public new static FolderExistsCondition FromNode(XmlNode conditionNode)
        {
            return new FolderExistsCondition(conditionNode);
        }

        public FolderExistsCondition(XmlNode conditionNode) : base(conditionNode)
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
            if (Directory.Exists(path))
            {
                if(matchcase)
                {
                    var origFoldername = new DirectoryInfo(path).Name;
                    var folders = Directory.GetParent(path).GetDirectories().OrderBy(f => f.Name).ToArray();
                    foreach (var folder in folders)
                    {
                        var foldername = folder.Name;
                        if(origFoldername.Equals(foldername,StringComparison.CurrentCultureIgnoreCase) && !origFoldername.Equals(foldername))
                        {
                            //Invalid casing
                            setFailureMessage(tokens, $"Folder exists but casing does not match (found {foldername})", potentialSuppressions);
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
                setFailureMessage(tokens, $"Folder not found at {path}", potentialSuppressions);
                return false;
            }
        }

        private string rawPath;
        private string rawMatchCase;
    }
}
