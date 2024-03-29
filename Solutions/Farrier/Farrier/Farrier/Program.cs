﻿using System;
using CommandLine;
using NLog;
using Farrier.Helpers;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Farrier.Parser;
using Farrier.Forge;
using Farrier.RoundUp;
using Farrier.Inspect;
using System.IO;
using System.Text.RegularExpressions;

namespace Farrier
{
    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static LogRouter log = new LogRouter((e, s) => logger.Error(e, s),
                                        s => logger.Warn(s),
                                        s => logger.Info(s),
                                        s => logger.Debug(s));

        static void Main(string[] args)
        {
            ExitCode exitcode;
            try
            {
                //TEMPORARY DEFAULT VALUES
                if (System.Diagnostics.Debugger.IsAttached && args.Length == 0)
                {
                    //args = @"forge -b Samples/ListFormatting/Playground.xml --listtokens".Split();

                    //args = new string[] { "inspect", "-c", "Samples/ListFormatting/LFSampleValidation.xml", "-r", "ValidateSamples", "-s", @"D:\Code\PnP\sp-dev-list-formatting\" };
                    //args = @"roundup -m Samples/ListFormatting/LFAssetMap.xml -s D:\Code\PnP\sp-dev-list-formatting -j sample.json --overwrite --pathdepth 3 --joinedfilename samples.json -o D:\Code\PnP\Community-Tooling\Solutions\Farrier\Farrier\Farrier\Samples\ListFormatting -f LFSamples.csv".Split();
                    //args = @"forge -b Samples/ListFormatting/LFForgeBlueprint.xml --listtokens -o D:\code\pnp\sp-dev-list-formatting\docs\ -f sp-field-border".Split();
                    args = @"fromfile -f Samples/ListFormatting/Farrier.txt".Split();
                }

                CommandLine.Parser.Default.ParseArguments(args, LoadVerbs())
                    .WithParsed(PerformOperation);

                exitcode = ExitCode.Success;
            }
            catch (Exception e)
            {
                logger.Error(e, e.Message + e.StackTrace);
                exitcode = ExitCode.Failure;
            }

            Environment.Exit((int)exitcode);
        }

        private static Type[] LoadVerbs()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();
        }

        private static void PerformOperation(object obj)
        {
            switch(obj)
            {
                case InspectOptions i:
                    RunInspect(i);
                    break;
                case ForgeOptions f:
                    RunForge(f);
                    break;
                case RoundUpOptions r:
                    RunRoundUp(r);
                    break;
                case FromFileOptions ff:
                    RunFromFile(ff);
                    break;
                case CopyFileOptions c:
                    CopyFile(c);
                    break;
            }

            void RunFromFile(FromFileOptions options)
            {
                logger.Info("Running from file!");
                log.Debug($"Param: file={options.File}");

                if (!File.Exists(options.File))
                {
                    log.Error($"File not found at: {options.File}");
                    return;
                }

                var commands = File.ReadAllLines(options.File);
                foreach (var command in commands)
                {
                    if(!command.StartsWith('#'))
                    {
                        var parameters = Regex.Matches(command, @"[\""].+?[\""]|[^ ]+")
                                     .Cast<Match>()
                                     .Select(x => x.Value.Trim('"'));
                        CommandLine.Parser.Default.ParseArguments(parameters, LoadVerbs()).WithParsed(PerformOperation);
                    }
                }
            }

            void CopyFile(CopyFileOptions options)
            {
                logger.Info("Copying a file!");
                log.Debug($"Param: file={options.File}");
                log.Debug($"Param: outputpath={options.OutputPath}");
                log.Debug($"Param: overwrite={options.Overwrite}");

                var source = new FileInfo(options.File);
                if (!source.Exists)
                {
                    log.Error($"File not found at: {options.File}");
                    return;
                }
                if (!Directory.Exists(options.OutputPath))
                {
                    log.Error($"Directory not found at: {options.OutputPath}");
                    return;
                }
                var destination = Path.Combine(options.OutputPath, source.Name);
                if(File.Exists(destination) && !options.Overwrite)
                {
                    log.Error($"File already exists at destination. Add overwrite option to do it anyway.");
                    return;
                }
                source.CopyTo(destination, options.Overwrite);
                log.Info($"File copied to {destination}");
            }

            void RunInspect(InspectOptions options)
            {
                logger.Info("Inspecting!");
                log.Debug($"Param: ruleset={options.RuleSet}");
                log.Debug($"Param: startingpath={options.StartingPath}");
                log.Debug($"Param: rule={options.Rule}");
                log.Debug($"Param: outputpath={options.OutputPath}");
                log.Debug($"Param: tokens={options.Tokens}");
                log.Debug($"Param: listtokens={options.ListTokens}");
                log.Debug($"Param: skipxmlvalidation={options.SkipXMLValidation}");

                var i = new Inspector(options.RuleSet,
                                      options.StartingPath,
                                      options.Rule,
                                      options.OutputPath,
                                      TokenManager.IEnumerableToDictionary(options.Tokens),
                                      options.ListTokens,
                                      options.SkipXMLValidation,
                                      log);
                i.Inspect();
            }

            void RunForge(ForgeOptions options)
            {
                log.Info("Forging!");
                log.Debug($"Param: blueprint={options.Blueprint}");
                log.Debug($"Param: outputpath={options.OutputPath}");
                log.Debug($"Param: tokens={options.Tokens}");
                log.Debug($"Param: listtokens={options.ListTokens}");
                log.Debug($"Param: skipxmlformattingfix={options.SkipXMLFormattingFix}");
                log.Debug($"Param: skipxmlvalidation={options.SkipXMLValidation}");
                log.Debug($"Param: file={options.File}");

                var f = new Forger(options.Blueprint,
                                   options.OutputPath,
                                   TokenManager.IEnumerableToDictionary(options.Tokens),
                                   options.ListTokens,
                                   options.SkipXMLFormattingFix,
                                   options.SkipXMLValidation,
                                   log);
                f.Forge(options.File);
            }

            void RunRoundUp(RoundUpOptions options)
            {
                log.Info("Rounding Up!");
                log.Debug($"Param: map={options.Map}");
                log.Debug($"Param: outputpath={options.OutputPath}");
                log.Debug($"Param: outputfilename={options.OutputFilename}");
                log.Debug($"Param: startpath={options.StartPath}");
                log.Debug($"Param: jsonfilepattern={options.JSONFilePattern}");
                log.Debug($"Param: listjsonfiles={options.ListJSONFiles}");
                log.Debug($"Param: tokens={options.Tokens}");
                log.Debug($"Param: listtokens={options.ListTokens}");
                log.Debug($"Param: overwrite={options.Overwrite}");
                log.Debug($"Param: skipheaders={options.SkipHeaders}");
                log.Debug($"Param: firstonly={options.FirstOnly}");
                log.Debug($"Param: multivalueseparator={options.MultiValueSeparator}");
                log.Debug($"Param: skip={options.Skip}");
                log.Debug($"Param: limit={options.Limit}");
                log.Debug($"Param: pathdepth={options.PathDepth}");
                log.Debug($"Param: skipxmlvalidation={options.SkipXMLValidation}");
                log.Debug($"Param: joinedfilename={options.JoinedFilename}");

                var w = new Wrangler(options.Map,
                                     options.OutputPath,
                                     options.OutputFilename,
                                     options.StartPath,
                                     options.JSONFilePattern,
                                     options.ListJSONFiles,
                                     TokenManager.IEnumerableToDictionary(options.Tokens),
                                     options.ListTokens,
                                     options.Overwrite,
                                     options.SkipHeaders,
                                     options.FirstOnly,
                                     options.MultiValueSeparator,
                                     options.Skip,
                                     options.Limit,
                                     options.PathDepth,
                                     options.SkipXMLValidation,
                                     options.JoinedFilename,
                                     log);
                w.RoundUp();
            }
        }

    }
}
