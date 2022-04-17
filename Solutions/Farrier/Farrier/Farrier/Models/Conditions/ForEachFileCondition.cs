﻿using System;
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

            var directory = new DirectoryInfo(path);
            var searchPattern = tokens.DecodeString(Pattern);
            var files = directory.GetFiles(searchPattern);
            var currentfile = 1;
            var totalfiles = files.Length;
            foreach (var file in files)
            {
                Info(tokens, $"Processing file ({currentfile}/{totalfiles}): {file.Name}...", prefix);

                var foreachTokens = new TokenManager(tokens);
                foreachTokens.NestToken("Each", file.Name);
                foreachTokens.NestToken("ContainerPath", directory.FullName);
                foreachTokens.NestToken("ContainerName", directory.Name);
                foreachTokens.NestToken("FilePath", file.FullName);
                foreachTokens.NestToken("FileExtension", file.Extension.Trim('.'));
                foreachTokens.NestToken("FileNoExtension", file.Name.Replace(file.Extension, ""));

                foreach (var condition in _subConditions)
                {
                    var result = condition.IsValid(foreachTokens, runRule, parentRule, prefix+1, directory.FullName);

                    if (!result)
                    {
                        if (condition.IsWarning)
                        {
                            //Log as warning, but don't fail the condition
                            Warn(foreachTokens, condition.FailureMessage, prefix+1);
                        }
                        else
                        {
                            //no need to keep evaluating if even 1 sub is false
                            if(!condition.SuppressFailureMessage)
                            {
                                Error(foreachTokens, condition.FailureMessage, prefix + 1);
                            }
                            this.setFailureMessage(tokens, $"Sub Condition failure during processing of file {file.FullName}");
                            return false;
                        }
                    }
                    currentfile += 1;
                }
            }
            return true;
        }

        public readonly string Path;
        public readonly string Pattern;

    }
}
