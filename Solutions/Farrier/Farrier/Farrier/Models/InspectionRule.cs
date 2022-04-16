﻿using System;
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

        public InspectionRule(string name, string description = "", LogRouter log = null)
        {
            if (log == null)
                _log = new LogRouter();
            else
                _log = log;

            Name = name;
            Description = description;

            tokens = new TokenManager(new FunctionResolver(), log: _log);
            this.messages = new List<Message>();
        }

        public InspectionRule(TokenManager rootTokens, XmlNode ruleNode, LogRouter log = null)
        {
            if (log == null)
                _log = new LogRouter();
            else
                _log = log;


            Name = XmlHelper.XmlAttributeToString(ruleNode.Attributes["name"]);
            Description = XmlHelper.XmlAttributeToString(ruleNode.Attributes["description"]);

            tokens = new TokenManager(rootTokens);

            _tokensNode = ruleNode.SelectSingleNode("tokens");

            _conditionsNode = ruleNode.SelectSingleNode("conditions");
            if(_conditionsNode == null || _conditionsNode.ChildNodes.Count <= 0)
            {
                _log.Warn($"No conditions found for rule: {this.Name}");
            }
            this.messages = new List<Message>();
        }

        public bool Run(Dictionary<string, string> ruleTokens, TokenManager rootTokens, DelRunRule runRule, bool listTokens = false, int prefix = 0, string startingpath = "", InspectionRule parentRule = null)
        {
            _tokensDictionary = ruleTokens;
            return Run(rootTokens, runRule, listTokens, prefix, startingpath, parentRule);
        }

        public bool Run(TokenManager rootTokens, DelRunRule runRule, bool listTokens = false, int prefix = 0, string startingpath = "", InspectionRule parentRule = null)
        {
            _log.Info($"Running rule \"{Name}\"...",prefix);
            tokens.Reset();
            tokens.AddTokens(rootTokens.CleanTokens());

            //Process tokens (done here so that parent token values can be evaluated on the fly)
            if (parentRule != null)
            {
                tokens.AddToken("ParentRuleName", parentRule.Name);
                if (_tokensDictionary != null)
                    tokens.AddTokens(_tokensDictionary, false, rootTokens, parentRule.tokens);
                if (_tokensNode != null)
                    tokens.AddTokens(_tokensNode, false, rootTokens, parentRule.tokens);
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

            var result = rootCondition.IsValid(tokens, runRule, this, prefix, 0, startingpath);
            this.messages.AddRange(rootCondition.Messages);
            
            if(!result)
            {
                if(rootCondition.IsWarning)
                {
                    messages.Add(Message.Warning(tokens.DecodeString(rootCondition.FailureMessage)));
                }
                else
                {
                    if(!rootCondition.SuppressFailureMessage)
                    {
                        this.messages.Add(Message.Error(tokens.DecodeString(rootCondition.FailureMessage)));
                    }
                    return false;
                }
            }
            return true;
        }

        public string Name { get; }
        public string Description { get; }

        public TokenManager tokens { get; }

        protected List<Message> messages;
        public List<Message> Messages { get { return this.messages; } }
    }
}
