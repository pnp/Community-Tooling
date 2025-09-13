using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.IO;
using System.Linq;

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
            if(string.IsNullOrEmpty(Pattern))
            {
                Pattern = "*";
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

            int skip = ValidateSkip(tokens, "files", prefix);
            int limit = ValidateLimit(tokens, "files", prefix);
            var quiet = tokens.DecodeString(rawQuiet) == "true";

            var path = PathNormalizer.Normalize(System.IO.Path.Combine(startingpath, tokens.DecodeString(Path)));
            propertyMap.Add("path", path);

            if(!Directory.Exists(path))
            {
                setFailureMessage(tokens, $"Unable to find the directory {path}", potentialSuppressions);
                return false;
            }

            bool success = true;
            var directory = new DirectoryInfo(path);
            var searchPattern = tokens.DecodeString(Pattern);
            var files = directory.GetFiles(searchPattern).OrderBy(f => f.Name).ToArray();
            if (skip > 0)
                files = files.Skip(skip).ToArray();
            if (limit > 0 && files.Length > limit)
                files = files.SkipLast(files.Length - limit).ToArray();

            var currentfile = 1;
            var totalfiles = files.Length;
            foreach (var file in files)
            {
                if (!quiet)
                    childMessages.Add(new Message(MessageLevel.info, Name, $"File ({currentfile}/{totalfiles}): {file.Name}", prefix));

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
                    childMessages.AddRange(condition.Messages);

                    if (condition.IgnoreResult)
                        continue;

                    if (!result)
                    {
                        if (condition.IsWarning)
                        {
                            //Log as warning, but don't fail the condition
                            if (!condition.SuppressFailureMessage)
                            {
                                childMessages.Add(new Message(MessageLevel.warning, condition.Name, tokens.DecodeString(condition.FailureMessage), prefix + 1));
                            }
                        }
                        else
                        {
                            //no need to keep evaluating if even 1 sub is false
                            if(!condition.SuppressFailureMessage)
                            {
                                childMessages.Add(new Message(MessageLevel.error, condition.Name, tokens.DecodeString(condition.FailureMessage), prefix + 1));
                            }
                            setFailureMessage(tokens, $"Sub Condition failure during processing of file {file.FullName}", potentialSuppressions);
                            success = false;
                            break;
                        }
                    }
                }
                if (!success)
                    break;
                currentfile += 1;
            }
            LogChildMessages(success);
            return success;
        }

        public readonly string Path;
        public readonly string Pattern;

    }
}
