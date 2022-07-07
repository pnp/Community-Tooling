using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace Farrier.Helpers
{
    [Verb("copyfile", HelpText = "Copies a file from one location to another")]
    class CopyFileOptions
    {
        [Option('f', "file", Required = true, HelpText = "Path of the file to copy")]
        public string File { get; set; }

        [Option('o', "outputpath", Required = false, Default = "", HelpText = "Directory where the file should be copied. Local directory used when not specified.")]
        public string OutputPath { get; set; }

        [Option(Required = false, Default = false, HelpText = "Add to overwrite the destination file even if it exists. When not specified, if the file already exists, the process will fail (and the file will be left alone).")]
        public bool Overwrite { get; set; }
    }
}
