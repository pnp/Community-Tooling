using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace Farrier.Helpers
{
    [Verb("roundup", HelpText="Extracts JSON values to CSV")]
    class RoundUpOptions
    {
        [Option('m', "map", Required = true, HelpText = "The config file that defines the JSON path to column mapping")]
        public string Map { get; set; }

        [Option('o', "outputpath", Required = false, Default = "", HelpText = "The directory where files should be written, written locally when unspecified")]
        public string OutputPath { get; set; }

        [Option('j', "jsonfilepath", Required = false, Default = "*.json", HelpText = "The path to the JSON file(s) to round up. When not specified, all JSON files in the current directory are used. Use * for path wildcards.")]
        public string JSONFilePath { get; set; }
    }
}
