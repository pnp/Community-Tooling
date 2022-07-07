using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace Farrier.Helpers
{
    [Verb("fromfile", HelpText="Executes commands from a file (each command on a separate line)")]
    class FromFileOptions
    {
        [Option('f', "file", Required = true, HelpText = "The text file containing the commands to execute (one per line)")]
        public string File { get; set; }
    }
}
