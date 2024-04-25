using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.IO;
using System.Linq;

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
            rawPath = XmlHelper.XmlAttributeToString(conditionNode.Attributes["path"]);
            rawPattern = XmlHelper.XmlAttributeToString(conditionNode.Attributes["pattern"]);
            if(String.IsNullOrEmpty(rawPattern))
            {
                rawPattern = "*";
            }
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule, InspectionRule parentRule, int prefix = 0, string startingpath = "")
        {
            messages.Clear();
            propertyMap.Clear();
            var potentialSuppressions = parentRule.GetSuppressionsForCondition(this);

            if (_subConditions.Count == 0)
            {
                return false;
            }

            int skip = ValidateSkip(tokens, "folders", prefix);
            int limit = ValidateLimit(tokens, "folders", prefix);
            var quiet = tokens.DecodeString(rawQuiet) == "true";

            var path = System.IO.Path.Combine(startingpath, tokens.DecodeString(rawPath));
            propertyMap.Add("path", path);

            if(!Directory.Exists(path))
            {
                this.setFailureMessage(tokens, $"Unable to find the directory {path}", potentialSuppressions);
                return false;
            }

            bool success = true;
            var directory = new DirectoryInfo(path);
            var searchPattern = tokens.DecodeString(rawPattern);
            var folders = directory.GetDirectories(searchPattern).OrderBy(f => f.Name).ToArray();
            if (skip > 0)
                folders = folders.Skip(skip).ToArray();
            if (limit > 0 && folders.Length > limit)
                folders = folders.SkipLast(folders.Length - limit).ToArray();

            var currentfolder = 1;
            var totalfolders = folders.Length;
            foreach (var folder in folders)
            {
                if (!quiet)
                    childMessages.Add(new Message(MessageLevel.info, Name, $"Folder ({currentfolder}/{totalfolders}): {folder.Name}", prefix));

                var foreachTokens = new TokenManager(tokens);
                foreachTokens.NestToken("Each", folder.Name);
                foreachTokens.NestToken("ContainerPath", directory.FullName);
                foreachTokens.NestToken("ContainerName", directory.Name);

                foreach (var condition in _subConditions)
                {
                    var result = condition.IsValid(foreachTokens, runRule, parentRule, prefix+1, folder.FullName);
                    childMessages.AddRange(condition.Messages);

                    if (condition.IgnoreResult)
                        continue;

                    if (!result)
                    {
                        if (condition.IsWarning)
                        {
                            //Log a warning, but don't fail the condition
                            if (!condition.SuppressFailureMessage)
                            {
                                childMessages.Add(new Message(MessageLevel.warning, condition.Name, tokens.DecodeString(condition.FailureMessage), prefix + 1));
                            }
                        }
                        else
                        {
                            //no need to keep evaluating if even 1 sub is false
                            if (!condition.SuppressFailureMessage)
                            {
                                childMessages.Add(new Message(MessageLevel.error, condition.Name, tokens.DecodeString(condition.FailureMessage), prefix+1));
                            }
                            this.setFailureMessage(tokens, $"Sub Condition failure during processing of folder {folder.FullName}", potentialSuppressions);
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

        public readonly string rawPath;
        public readonly string rawPattern;

    }
}
