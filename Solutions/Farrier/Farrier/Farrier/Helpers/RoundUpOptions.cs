using System;
using System.Collections.Generic;
using CommandLine;

namespace Farrier.Helpers
{
    [Verb("roundup", HelpText="Extracts JSON values to CSV")]
    class RoundUpOptions
    {
        [Option('m', "map", Required = true, HelpText = "The config file that defines the JSON path to column mapping.")]
        public string Map { get; set; }

        [Option('o', "outputpath", Required = false, Default = "", HelpText = "The directory where files should be written, written locally when unspecified.")]
        public string OutputPath { get; set; }

        [Option('f', "outputfilename", Required = false, Default = "roundup.csv", HelpText = "The name of the CSV file. When unspecified, roundup.csv is used")]
        public string OutputFilename { get; set; }

        [Option('s', "startpath", Required = false, Default = "", HelpText = "The directory to search for json files in. When unspecified the current directory is used.")]
        public string StartPath { get; set; }

        [Option('j', "jsonfilepattern", Required = false, Default = "*.json", HelpText = "The file search pattern to use to find JSON file(s) to round up. When not specified, all JSON files in the current directory are used. Use * for path wildcards.")]
        public string JSONFilePattern { get; set; }

        [Option(Required = false, Default = false, HelpText = "Add to have all targeted JSON file paths listed during processing (helpful for debugging)")]
        public bool ListJSONFiles { get; set; }

        [Option('t', "tokens", Required = false, Separator = ',', Min = 2, HelpText = "Comma separated list of token key followed by value for when you want dynamic tokens not specified in the map")]
        public IEnumerable<string> Tokens { get; set; }

        [Option(Required = false, Default = false, HelpText = "Add to have all token values listed during processing (helpful for debugging)")]
        public bool ListTokens { get; set; }

        [Option(Required = false, Default = false, HelpText = "Add to overwrite the resulting csv file even if it exists. When not specified, if the file already exists, the process will fail (and the file will be left alone).")]
        public bool Overwrite { get; set; }

        [Option(Required = false, Default = false, HelpText = "By default headers will be included in the CSV, add this parameter to leave them out.")]
        public bool SkipHeaders { get; set; }

        [Option(Required = false, Default = false, HelpText = "Add to only take the first matching value for a path. Default is to include all found values with the specified multivalueseparater between them.")]
        public bool FirstOnly { get; set; }

        [Option(Required = false, Default = "|", HelpText = "When not specifying FirstOnly, when multiple values are found for a given path they will all be including with this value between them.")]
        public string MultiValueSeparator { get; set; }

        [Option(Required = false, Default = 0, HelpText = "Number of files to skip processing")]
        public int Skip { get; set; }

        [Option(Required = false, Default = 10000, HelpText = "Maximum number of files to process (default = 10,000)")]
        public int Limit { get; set; }

        [Option(Required = false, Default = 0, HelpText = "Amount of path to use when referring to the file being processed. 0 = filename, 1 = parentdirectory/filename, 2 = parentofparent/parentdirectory/filename, etc.")]
        public int PathDepth { get; set; }

        [Option(Required = false, Default = false, HelpText = "Add to skip XML validation for the map file (not recommended)")]
        public bool SkipXMLValidation { get; set; }

        [Option(Required = false, HelpText = "When included, all JSON files will be written to a single JSON file as an array of objects.")]
        public string JoinedFilename { get; set; }
    }
}
