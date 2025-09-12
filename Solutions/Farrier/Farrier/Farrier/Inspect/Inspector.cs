using System;
using System.Collections.Generic;
using System.Text;
using Farrier.Parser;
using Farrier.Helpers;
using System.Xml;
using System.Xml.Schema;
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
        private bool _skipXMLValidation;

        private Dictionary<string, InspectionRule> _rules;

        public Inspector(string configpath, string startingpath = "", string rule = "", string outputpath = "", Dictionary<string, string> tokens = null, bool ListTokens = false, bool SkipXMLValidation = false, LogRouter log = null)
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
            _skipXMLValidation = SkipXMLValidation;

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
                XmlNamespaceManager nsmgr;
                try
                {
                    doc.Load(_configpath);
                    nsmgr = new XmlNamespaceManager(doc.NameTable);
                    nsmgr.AddNamespace("f", "https://pnp.github.io/inspection");

                    if(!_skipXMLValidation)
                    {
                        var validationMessages = XmlHelper.ValidateSchema(_configpath, "https://pnp.github.io/inspection", @"XML\Inspection.xsd");
                        if (validationMessages.Count > 0)
                        {
                            _log.Error("Invalid Inspection Configuration XML:");
                            foreach (var message in validationMessages)
                            {
                                _log.Error($"  {message}");
                            }
                            _log.Warn("If you are convinced the validation errors above will not affect the actual inspection, run again with --skipxmlvalidation to ignore these messages");
                            return;
                        }
                    }
                    else
                    {
                        _log.Warn("Skipping XML Validation of Inspection Configuration file (Proceed at your own risk)");
                    }
                }
                catch(XmlException ex)
                {
                    _log.Error($"Unable to read {_configpath}, likely bad XML. Details: {ex.Message}");
                    return;
                }

                _rootTokens.AddTokens(doc.SelectSingleNode("//f:tokens", nsmgr));
                if (_listTokens)
                    _rootTokens.LogTokens();

                XmlNodeList ruleNodes = doc.SelectNodes("//f:rule", nsmgr);
                if (ruleNodes != null && ruleNodes.Count > 0)
                {
                    _log.Info($"Processing {ruleNodes.Count} rules with {XmlHelper.CountChildrenRecursively(ruleNodes)} sub nodes");

                    //Build the rules from the ruleset
                    _rules = new Dictionary<string, InspectionRule>();
                    foreach (XmlNode ruleNode in ruleNodes)
                    {
                        var rule = new InspectionRule(_rootTokens, ruleNode, nsmgr, log: _log);
                        if(string.IsNullOrEmpty(_rule))
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
                if(!rule.Quiet)
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

        private InspectionRule RunChildRule(string ruleName, Dictionary<string, string> ruleTokens, int prefix, InspectionRule parentRule, bool quiet = false)
        {
            if(_rules.ContainsKey(ruleName))
            {
                var rule = _rules[ruleName];
                var result = rule.Run(ruleTokens, _rootTokens, RunChildRule, _listTokens, prefix, _startingpath, parentRule, quiet);
                if(!(rule.Quiet || quiet))
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
