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
        [Option('m', "map", Required = true, HelpText = "The config file that defines the JSON path to column mapping.")]
        public string Map { get; set; }

        [Option('o', "outputpath", Required = false, Default = "", HelpText = "The directory where files should be written, written locally when unspecified.")]
        public string OutputPath { get; set; }

        [Option('s', "startpath", Required = false, Default = "", HelpText = "The directory to search for json files in. When unspecified the current directory is used.")]
        public string StartPath { get; set; }

        [Option('j', "jsonfilepattern", Required = false, Default = "*.json", HelpText = "The file search pattern to use to find JSON file(s) to round up. When not specified, all JSON files in the current directory are used. Use * for path wildcards.")]
        public string JSONFilePattern { get; set; }
    }
}
