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
            rawPath = XmlHelper.XmlAttributeToString(conditionNode.Attributes["path"]);
            rawMatchCase = XmlHelper.XmlAttributeToString(conditionNode.Attributes["matchcase"]);
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule = null, InspectionRule parentRule = null, int prefix = 0, int messagePrefix = 0, string startingpath = "")
        {
            this.messages = new List<Message>();
            var path = System.IO.Path.Combine(startingpath, tokens.DecodeString(rawPath));
            var matchcase = tokens.DecodeString(rawMatchCase) == "true";
            if (File.Exists(path))
            {
                if(matchcase)
                {
                    var origFilename = System.IO.Path.GetFileName(path);
                    var files = Directory.GetFiles(System.IO.Path.GetDirectoryName(path));
                    foreach (var file in files)
                    {
                        var filename = System.IO.Path.GetFileName(file);
                        if(origFilename.Equals(filename,StringComparison.CurrentCultureIgnoreCase) && !origFilename.Equals(filename))
                        {
                            //Invalid casing
                            this.setFailureMessage(tokens, $"File exists but casing does not match (found {filename})");
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
                this.setFailureMessage(tokens, $"File not found at {path}");
                return false;
            }
        }

        private string rawPath;
        public string rawMatchCase;
    }
}
