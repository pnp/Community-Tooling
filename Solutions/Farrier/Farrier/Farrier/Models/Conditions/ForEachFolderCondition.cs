using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.IO;

namespace Farrier.Models.Conditions
{
    class ForEachFolderCondition : BaseForEachCondition
    {
        public new static ForEachFolderCondition FromNode(XmlNode conditionNode)
        {
            return new ForEachFolderCondition(conditionNode);
        }

        public ForEachFolderCondition(XmlNode conditionNode) : base(conditionNode)
        {
            Path = XmlHelper.XmlAttributeToString(conditionNode.Attributes["path"]);
            Pattern = XmlHelper.XmlAttributeToString(conditionNode.Attributes["pattern"]);
            if(String.IsNullOrEmpty(Pattern))
            {
                Pattern = "*";
            }
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, string startingpath = "")
        {

            if(_subConditions.Count == 0)
            {
                return false;
            }

            var path = System.IO.Path.Combine(startingpath, tokens.DecodeString(Path));

            if(!Directory.Exists(path))
            {
                if(String.IsNullOrEmpty(failuremessage))
                {
                    failuremessage = $"Unable to find the directory {path}";
                }
                return false;
            }

            var directory = new DirectoryInfo(path);

            var folders = directory.GetDirectories(Pattern);
            foreach (var folder in folders)
            {
                var foreachTokens = new TokenManager(tokens);
                foreachTokens.AddToken("Each", folder.Name);

                foreach (var condition in _subConditions)
                {
                    var result = condition.IsValid(foreachTokens, runRule, parentRule, prefix, folder.FullName);

                    //bubble up any warnings or errors
                    this.warnings.AddRange(condition.Warnings);
                    this.errors.AddRange(condition.Errors);

                    if (!result)
                    {
                        if (condition.IsWarning)
                        {
                            //Add its warning, but don't fail the condition
                            warnings.Add(foreachTokens.DecodeString(condition.FailureMessage));
                        }
                        else
                        {
                            //no need to keep evaluating if even 1 sub is false
                            errors.Add(foreachTokens.DecodeString(condition.FailureMessage));
                            if (String.IsNullOrEmpty(failuremessage))
                            {
                                failuremessage = $"Sub Condition failure during processing of folder {folder.FullName}";
                            }
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public readonly string Path;
        public readonly string Pattern;

    }
}
