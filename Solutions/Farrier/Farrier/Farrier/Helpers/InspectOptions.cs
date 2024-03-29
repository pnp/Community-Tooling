﻿using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace Farrier.Helpers
{
    [Verb("inspect", isDefault: true, HelpText="Validates folders and files using a ruleset")]
    class InspectOptions
    {
        [Option('c', Required = true, HelpText = "The config file containing the rules to use")]
        public string RuleSet { get; set; }

        [Option('s', Required = false, Default = "", HelpText = "The directory to start in, uses the current directory when unspecified")]
        public string StartingPath { get; set; }

        [Option('r', Required = false, HelpText = "The name of the rule to run, uses the first rule when unspecified")]
        public string Rule { get; set; }

        [Option('o', Required = false, Default = "", HelpText = "The directory where results should be written, written locally when unspecified")]
        public string OutputPath { get; set; }
  
        [Option('t', Required = false, Separator = ',', Min = 2, HelpText = "Comma separated list of token key followed by value for when you want dynamic tokens not specified in the ruleset")]
        public IEnumerable<string> Tokens { get; set; }

        [Option(Required = false, Default = false, HelpText = "Add to have all token values listed during processing (helpful for debugging)")]
        public bool ListTokens { get; set; }

        [Option(Required = false, Default = false, HelpText = "Add to skip XML validation for the configuration file (not recommended)")]
        public bool SkipXMLValidation { get; set; }
    }
}
