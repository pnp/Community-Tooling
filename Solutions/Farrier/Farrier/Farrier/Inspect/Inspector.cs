using System;
using System.Collections.Generic;
using System.Text;
using Farrier.Parser;
using Farrier.Helpers;
using System.Xml;
using System.IO;
using Farrier.Models;

namespace Farrier.Inspect
{
    class Inspector
    {
        private LogRouter _log;

        private FunctionResolver _functionResolver;
        private TokenManager _rootTokens;

        private string _configpath;
        private string _startingpath;
        private string _rule;
        private string _outputpath;
        private bool _listTokens;

        private Dictionary<string, InspectionRule> _rules;

        public Inspector(string configpath, string startingpath = "", string rule = "", string outputpath = "", Dictionary<string, string> tokens = null, bool ListTokens = false, LogRouter log = null)
        {
            if (log == null)
                _log = new LogRouter();
            else
                _log = log;

            _configpath = configpath;
            _startingpath = startingpath;
            _rule = rule;
            _outputpath = outputpath;
            _listTokens = ListTokens;

            _functionResolver = new FunctionResolver(log: _log);
            _rootTokens = new TokenManager(_functionResolver, log: _log);

            // Add any tokens passed in the options
            _rootTokens.AddTokens(tokens);

            // Add default tokens
            _rootTokens.AddToken("StartingPath", startingpath);
        }

        public void Inspect()
        {
            try
            {
                if(!Directory.Exists(_startingpath))
                {
                    _log.Error($"No directory found for starting path: {_startingpath}");
                    return;
                }

                var doc = new XmlDocument();
                doc.Load(_configpath);

                _rootTokens.AddTokens(doc.SelectSingleNode("//tokens"));
                if (_listTokens)
                    _rootTokens.LogTokens();

                XmlNodeList ruleNodes = doc.SelectNodes("//rule");
                if (ruleNodes != null && ruleNodes.Count > 0)
                {
                    _log.Info($"Processing {ruleNodes.Count} rules");

                    //Build the rules from the ruleset
                    _rules = new Dictionary<string, InspectionRule>();
                    foreach (XmlNode ruleNode in ruleNodes)
                    {
                        var rule = new InspectionRule(_rootTokens, ruleNode, log: _log);
                        if(String.IsNullOrEmpty(_rule))
                        {
                            //When not specified, the first rule is used
                            _rule = rule.Name;
                        }

                        if(_rules.ContainsKey(rule.Name))
                        {
                            _log.Warn($"Rule already defined with name {rule.Name}, only using the first one!");
                        }
                        else
                        {
                            _rules.Add(rule.Name, rule);
                        }
                    }

                    //Let's get started!
                    var result = RunRule(_rule, null, 1);
                    _log.Info($"Inspection {(result ? "PASSED" : "FAILED")}");
                }
                else
                {
                    _log.Warn("No rule entries found in config!");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Error performing inspection using ruleset: \"{_configpath}\"");
            }
        }

        private bool RunRule(string ruleName, Dictionary<string, string> ruleTokens = null, int prefix = 0)
        {
            if (_rules.ContainsKey(ruleName))
            {
                var rule = _rules[ruleName];
                var result = rule.Run(ruleTokens, _rootTokens, RunChildRule, _listTokens, prefix, _startingpath);
                rule.messages.Add(new Message(MessageLevel.info, ruleName, $"Rule \"{ruleName}\" {(result ? "PASSED" : "FAILED")}", prefix));
                rule.LogMessages();

                return result;
            }
            else
            {
                _log.Warn($"Rule {ruleName} not found!", prefix);
                return false;
            }
        }

        private InspectionRule RunChildRule(string ruleName, Dictionary<string, string> ruleTokens, int prefix, InspectionRule parentRule)
        {
            if(_rules.ContainsKey(ruleName))
            {
                var rule = _rules[ruleName];
                var result = rule.Run(ruleTokens, _rootTokens, RunChildRule, _listTokens, prefix, _startingpath, parentRule);
                rule.messages.Add(new Message(MessageLevel.info, ruleName, $"-Rule \"{ruleName}\" {(result ? "PASSED" : "FAILED")}", prefix));

                return rule;
            }
            else
            {
                _log.Warn($"Rule {ruleName} not found!", prefix);
                return null;
            }
        }
    }
}
