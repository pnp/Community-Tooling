using System;
using System.Collections.Generic;
using System.Text;
using Farrier.Parser;
using System.Xml;
using Farrier.Helpers;
using Farrier.Models.Conditions;

namespace Farrier.Models
{
    class InspectionRule
    {
        private LogRouter _log;
        private XmlNode _tokensNode;
        private XmlNode _conditionsNode;
        private Dictionary<string, string> _tokensDictionary;
        public List<Message> messages;

        public InspectionRule(string name, string description = "", LogRouter log = null)
        {
            if (log == null)
                _log = new LogRouter();
            else
                _log = log;

            Name = name;
            Description = description;

            tokens = new TokenManager(new FunctionResolver(), log: _log);
            messages = new List<Message>();
        }

        public InspectionRule(TokenManager rootTokens, XmlNode ruleNode, XmlNamespaceManager nsmgr, LogRouter log = null)
        {
            if (log == null)
                _log = new LogRouter();
            else
                _log = log;


            Name = XmlHelper.XmlAttributeToString(ruleNode.Attributes["name"]);
            Description = XmlHelper.XmlAttributeToString(ruleNode.Attributes["description"]);
            Quiet = XmlHelper.XmlAttributeToBool(ruleNode.Attributes["quiet"]);

            tokens = new TokenManager(rootTokens);
            messages = new List<Message>();

            _tokensNode = ruleNode.SelectSingleNode("f:tokens", nsmgr);

            _conditionsNode = ruleNode.SelectSingleNode("f:conditions", nsmgr);
            if(_conditionsNode == null || _conditionsNode.ChildNodes.Count <= 0)
            {
               _log.Warn($"No conditions found for rule: {this.Name}");
            }
        }

        public bool Run(Dictionary<string, string> ruleTokens, TokenManager rootTokens, DelRunRule runRule, bool listTokens = false, int prefix = 0, string startingpath = "", InspectionRule parentRule = null, bool quiet = false)
        {
            _tokensDictionary = ruleTokens;
            return Run(rootTokens, runRule, listTokens, prefix, startingpath, parentRule, quiet);
        }

        public bool Run(TokenManager rootTokens, DelRunRule runRule, bool listTokens = false, int prefix = 0, string startingpath = "", InspectionRule parentRule = null, bool quiet = false)
        {
            messages.Clear();

            if(_conditionsNode == null)
            {
                messages.Add(new Message(MessageLevel.error, Name, "Unable to run rule without conditions, marking as failed.", prefix));
                return false;
            }

            if(!(Quiet || quiet))
                messages.Add(new Message(MessageLevel.info, Name, $"Running rule \"{Name}\"...", prefix));
            tokens.Reset();
            tokens.AddTokens(rootTokens.CleanTokens());

            //Process tokens (done here so that parent token values can be evaluated on the fly)
            if (parentRule != null)
            {
                tokens.AddToken("ParentRuleName", parentRule.Name);
                if (_tokensDictionary != null)
                    tokens.AddTokens(_tokensDictionary, false, rootTokens, parentRule.tokens);
                if (_tokensNode != null)
                    tokens.AddTokens(_tokensNode, false);
            }
            else
            {
                tokens.AddToken("ParentRuleName", "");
                if (_tokensDictionary != null)
                    tokens.AddTokens(_tokensDictionary, false, rootTokens);
                if (_tokensNode != null)
                    tokens.AddTokens(_tokensNode, false, rootTokens);
            }

            tokens.AddToken("RuleName", Name);

            if (listTokens)
                tokens.LogTokens(prefix+1);

            //Add conditions (always starts with an And condition)
            var rootCondition = AndCondition.FromNode(_conditionsNode);
            rootCondition.Name = this.Name;

            var result = rootCondition.IsValid(tokens, runRule, this, prefix, startingpath);
            messages.AddRange(rootCondition.Messages);
            var success = true;
            if(!result)
            {
                if(rootCondition.IsWarning)
                {
                    messages.Add(new Message(MessageLevel.warning, Name, tokens.DecodeString(rootCondition.FailureMessage), prefix));
                }
                else
                {
                    if(!rootCondition.SuppressFailureMessage)
                    {
                        messages.Add(new Message(MessageLevel.error, Name, tokens.DecodeString(rootCondition.FailureMessage), prefix));
                    }
                    success = false;
                }
            }
            Succeeded = success;
            return success;
        }

        public void LogMessages(bool skipErrors = false)
        {
            foreach (var message in messages)
            {
                var text = $"<{message.Source}> {message.Text}";
                switch (message.Level)
                {
                    case MessageLevel.warning:
                        _log.Warn(text, message.Prefix);
                        break;
                    case MessageLevel.error:
                        if (!skipErrors)
                        {
                            if (message.Ex != null)
                                _log.Error(message.Ex, text, message.Prefix);
                            else
                                _log.Error(text, message.Prefix);
                        }
                        break;
                    default:
                        _log.Info(text, message.Prefix);
                        break;
                }
            }
        }

        public string Name { get; }
        public string Description { get; }
        public bool Quiet { get; set; }

        public TokenManager tokens { get; }

        public bool Succeeded;
    }
}
