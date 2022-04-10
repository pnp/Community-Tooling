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

        public override bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, int messagePrefix = 0, string startingpath = "")
        {
            this.messages = new List<Message>();
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
                foreachTokens.NestToken("Each", folder.Name);
                foreachTokens.NestToken("ContainerPath", directory.FullName);
                foreachTokens.NestToken("ContainerName", directory.Name);
                messages.Add(Message.Info($"Processing folder {folder.Name}...",messagePrefix));

                foreach (var condition in _subConditions)
                {
                    var result = condition.IsValid(foreachTokens, runRule, parentRule, prefix+1, messagePrefix+1, folder.FullName);

                    //bubble up any warnings or errors
                    this.messages.AddRange(condition.Messages);

                    if (!result)
                    {
                        if (condition.IsWarning)
                        {
                            //Add its warning, but don't fail the condition
                            messages.Add(Message.Warning(foreachTokens.DecodeString(condition.FailureMessage),messagePrefix+1));
                        }
                        else
                        {
                            //no need to keep evaluating if even 1 sub is false
                            messages.Add(Message.Error(foreachTokens.DecodeString(condition.FailureMessage),messagePrefix+1));
                            if (String.IsNullOrEmpty(failuremessage))
                            {
                                failuremessage = $"Sub Condition failure during processing of folder {folder.FullName}";
                            }
                            return false;
                        }
                    }
                }
            }
            if (String.IsNullOrEmpty(successmessage))
            {
                successmessage = $"foreachfolder finished for {path}";
            }
            return true;
        }

        public readonly string Path;
        public readonly string Pattern;

    }
}
