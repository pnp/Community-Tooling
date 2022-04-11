using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.IO;

namespace Farrier.Models.Conditions
{
    class ForEachFileCondition : BaseParentCondition
    {
        public new static ForEachFileCondition FromNode(XmlNode conditionNode)
        {
            return new ForEachFileCondition(conditionNode);
        }

        public ForEachFileCondition(XmlNode conditionNode) : base(conditionNode)
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
                this.setFailureMessage(tokens, $"Unable to find the directory {path}");
                return false;
            }

            var directory = new DirectoryInfo(path);
            var searchPattern = tokens.DecodeString(Pattern);
            var files = directory.GetFiles(searchPattern);
            foreach (var file in files)
            {
                var foreachTokens = new TokenManager(tokens);
                foreachTokens.NestToken("Each", file.Name);
                foreachTokens.NestToken("ContainerPath", directory.FullName);
                foreachTokens.NestToken("ContainerName", directory.Name);
                foreachTokens.NestToken("FilePath", file.FullName);
                foreachTokens.NestToken("FileExtension", file.Extension.Trim('.'));
                foreachTokens.NestToken("FileNoExtension", file.Name.Replace(file.Extension, ""));
                messages.Add(Message.Info($"Processing file {file.Name}...",messagePrefix));

                foreach (var condition in _subConditions)
                {
                    var result = condition.IsValid(foreachTokens, runRule, parentRule, prefix+1, messagePrefix+1, directory.FullName);

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
                            if(!condition.SuppressFailureMessage)
                            {
                                messages.Add(Message.Error(foreachTokens.DecodeString(condition.FailureMessage), messagePrefix + 1));
                            }
                            this.setFailureMessage(tokens, $"Sub Condition failure during processing of file {file.FullName}");
                            return false;
                        }
                    }
                }
            }
            this.setSuccessMessage(tokens, $"foreachfile finished for {path}");
            return true;
        }

        public readonly string Path;
        public readonly string Pattern;

    }
}
