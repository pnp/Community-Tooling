using System;
using System.Collections.Generic;
using System.Text;
using Farrier.Parser;
using System.Xml;
using Farrier.Helpers;

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
            tokens.AddToken("RuleName", name);
            this.warnings = new List<string>();
            this.errors = new List<string>();
        }

        public InspectionRule(XmlNode ruleNode, LogRouter log = null)
        {
            if (log == null)
                _log = new LogRouter();
            else
                _log = log;


            Name = XmlHelper.XmlAttributeToString(ruleNode.Attributes["name"]);
            Description = XmlHelper.XmlAttributeToString(ruleNode.Attributes["description"]);

            tokens = new TokenManager(new FunctionResolver(), log: _log);
            tokens.AddToken("RuleName", Name);

            _tokensNode = ruleNode.SelectSingleNode("tokens");

            _conditionsNode = ruleNode.SelectSingleNode("conditions");
            if(_conditionsNode == null || _conditionsNode.ChildNodes.Count <= 0)
            {
                _log.Warn($"No conditions found for rule: {this.Name}");
            }
            this.warnings = new List<string>();
            this.errors = new List<string>();
        }

        public bool Run(Dictionary<string, string> ruleTokens, TokenManager rootTokens, DelRunRule runRule, bool listTokens = false, int prefix = 0, string startingpath = "", InspectionRule parentRule = null)
        {
            _tokensDictionary = ruleTokens;
            return Run(rootTokens, runRule, listTokens, prefix, startingpath, parentRule);
        }

        public bool Run(TokenManager rootTokens, DelRunRule runRule, bool listTokens = false, int prefix = 0, string startingpath = "", InspectionRule parentRule = null)
        {
            _log.Info($"Running rule {Name}",prefix);

            //Process tokens (done here so that parent token values can be evaluated on the fly)
            if (parentRule != null)
            {
                tokens.AddToken("ParentRuleName", parentRule.Name);
                if(_tokensNode != null)
                    tokens.AddTokens(_tokensNode, false, rootTokens, parentRule.tokens);
                if (_tokensDictionary != null)
                    tokens.AddTokens(_tokensDictionary, false, rootTokens, parentRule.tokens);
            }
            else
            {
                tokens.AddToken("ParentRuleName", "");
                if (_tokensNode != null)
                    tokens.AddTokens(_tokensNode, false, rootTokens);
                if (_tokensDictionary != null)
                    tokens.AddTokens(_tokensDictionary, false, rootTokens);
            }

            if (listTokens)
                tokens.LogTokens(prefix+1);

            //Add conditions (always starts with an And condition)
            var rootCondition = AndCondition.FromNode(_conditionsNode);

            var result = rootCondition.IsValid(tokens, runRule, prefix, startingpath);
            this.warnings.AddRange(rootCondition.Warnings);
            this.errors.AddRange(rootCondition.Errors);
            
            if(!result)
            {
                if(rootCondition.IsWarning)
                {
                    warnings.Add(tokens.DecodeString(rootCondition.FailureMessage));
                }
                else
                {
                    this.errors.Add(tokens.DecodeString(rootCondition.FailureMessage));
                    return false;
                }
            }
            return true;
        }

        public string Name { get; }
        public string Description { get; }

        public TokenManager tokens { get; }

        protected List<string> warnings;
        protected List<string> errors;
        public List<string> Warnings { get { return this.warnings; } }
        public List<string> Errors { get { return this.errors; } }
    }
}
