using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.IO;

namespace Farrier.Models.Conditions
{
    class ForEachFolderCondition : BaseParentCondition
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
            messages.Clear();
            if (_subConditions.Count == 0)
            {
                return false;
            }

            var path = System.IO.Path.Combine(startingpath, tokens.DecodeString(Path));

            if(!Directory.Exists(path))
            {
                this.setFailureMessage(tokens, $"Unable to find the directory {path}");
                return false;
            }

            bool success = true;
            var directory = new DirectoryInfo(path);
            var searchPattern = tokens.DecodeString(Pattern);
            var folders = directory.GetDirectories(searchPattern);
            var currentfolder = 1;
            var totalfolders = folders.Length;
            foreach (var folder in folders)
            {
                childMessages.Add(new Message(MessageLevel.info, Name, $"Folder ({currentfolder}/{totalfolders}): {folder.Name}", prefix));

                var foreachTokens = new TokenManager(tokens);
                foreachTokens.NestToken("Each", folder.Name);
                foreachTokens.NestToken("ContainerPath", directory.FullName);
                foreachTokens.NestToken("ContainerName", directory.Name);

                foreach (var condition in _subConditions)
                {
                    var result = condition.IsValid(foreachTokens, runRule, parentRule, prefix+1, folder.FullName);
                    childMessages.AddRange(condition.Messages);

                    if (!result)
                    {
                        if (condition.IsWarning)
                        {
                            //Log a warning, but don't fail the condition
                            childMessages.Add(new Message(MessageLevel.warning, condition.Name, tokens.DecodeString(condition.FailureMessage), prefix+1));
                        }
                        else
                        {
                            //no need to keep evaluating if even 1 sub is false
                            if (!condition.SuppressFailureMessage)
                            {
                                childMessages.Add(new Message(MessageLevel.error, condition.Name, tokens.DecodeString(condition.FailureMessage), prefix+1));
                            }
                            this.setFailureMessage(tokens, $"Sub Condition failure during processing of folder {folder.FullName}");
                            success = false;
                            break;
                        }
                    }
                }
                if (!success)
                    break;
                currentfolder += 1;
            }
            LogChildMessages(success);
            return success;
        }

        public readonly string Path;
        public readonly string Pattern;

    }
}
