using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.IO;

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
            Path = XmlHelper.XmlAttributeToString(conditionNode.Attributes["path"]);
            MatchCase = XmlHelper.XmlAttributeToBool(conditionNode.Attributes["matchcase"]);
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule = null, InspectionRule parentRule = null, int prefix = 0, string startingpath = "")
        {
            var path = System.IO.Path.Combine(startingpath, tokens.DecodeString(Path));
            if (File.Exists(path))
            {
                if(MatchCase)
                {
                    var origFilename = System.IO.Path.GetFileName(path);
                    var files = Directory.GetFiles(System.IO.Path.GetDirectoryName(path));
                    foreach (var file in files)
                    {
                        var filename = System.IO.Path.GetFileName(file);
                        if(origFilename.Equals(filename,StringComparison.CurrentCultureIgnoreCase) && !origFilename.Equals(filename))
                        {
                            //Invalid casing
                            if(String.IsNullOrEmpty(this.failuremessage))
                            {
                                this.failuremessage = $"File exists but casing does not match (found {filename})";
                            }
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
                if(String.IsNullOrEmpty(this.failuremessage))
                {
                    this.failuremessage = $"File not found at {path}";
                }
                return false;
            }
        }

        public readonly string Path;
        public readonly bool MatchCase;
    }
}
