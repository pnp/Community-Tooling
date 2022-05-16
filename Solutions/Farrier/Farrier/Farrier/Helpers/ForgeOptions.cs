using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace Farrier.Helpers
{
    [Verb("forge", HelpText="Generates files using a blueprint")]
    class ForgeOptions
    {
        [Option('b', "blueprint", Required = true, HelpText = "The config file to use for generation")]
        public string Blueprint { get; set; }

        [Option('o', "outputpath", Required = false, Default = "", HelpText = "The directory where files should be written, written locally when unspecified")]
        public string OutputPath { get; set; }

        [Option('t', "tokens", Required = false, Separator = ',', Min = 2, HelpText = "Comma separated list of token key followed by value for when you want dynamic tokens not specified in the blueprint")]
        public IEnumerable<string> Tokens { get; set; }

        [Option(Required = false, Default = false, HelpText = "Add to have all token values listed during processing (helpful for debugging)")]
        public bool ListTokens { get; set; }

        [Option(Required = false, Default = false, HelpText = "By default Farrier attempts to correct text content that has been entered in XML (extra lines, spaces, etc.) but add this parameter to use the text exactly as entered")]
        public bool SkipXMLFormattingFix { get; set; }

        [Option(Required = false, Default = false, HelpText = "Add to skip XML validation for the configuration file (not recommended)")]
        public bool SkipXMLValidation { get; set; }
    }
}
