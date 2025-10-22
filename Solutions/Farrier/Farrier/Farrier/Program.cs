using System;
using CommandLine;
using NLog;
using Farrier.Helpers;
using System.Reflection;
using System.Linq;
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
                                        s => logger.Debug(s),
                                        s => logger.Trace(s));

        static void Main(string[] args)
        {
            ExitCode exitcode;
            try
            {
                //TEMPORARY DEFAULT VALUES
                if (System.Diagnostics.Debugger.IsAttached && args.Length == 0)
                {
                    //args = @"forge -b Samples/ListFormatting/Playground.xml --listtokens".Split();

                    // List Formatting Sample Validation
                    //args = new string[] { "inspect", "-c", @"Samples/ListFormatting/LFSampleValidation.xml", "-r", "ValidateSamples", "-s", @"/Users/chriskent/Code/pnp/List-Formatting/" };

                    // List Formatting CSV & samples.json creation
                    //args = new string[] { "roundup", "-m", @"Samples/ListFormatting/LFAssetMap.xml", "-s", @"/Users/chriskent/Code/pnp/List-Formatting/", "-j", "sample.json", "--overwrite", "--pathdepth", "3", "--joinedfilename", "samples.json", "-o", @"Samples/ListFormatting", "-f", "LFSamples.csv" };

                    // Copy the samples.json to the List-Formatting repo
                    //args = new string[] { "copyfile", "-f", @"Samples/ListFormatting/samples.json", "-o", @"/Users/chriskent/Code/pnp/List-Formatting/", "--overwrite" };

                    // Generate the docs in the List-Formatting repo
                    //args = new string[] { "forge", "-b", @"Samples/ListFormatting/LFForgeBlueprint.xml", "--listtokens", "-o", @"/Users/chriskent/Code/pnp/List-Formatting/docs/" };

                    // Run the List Formatting roundup, copyfile, and forget using a command file
                    args = new string[] { "fromfile", "-f", @"Samples/ListFormatting/Farrier.txt" };
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
                var filePath = PathNormalizer.Normalize(options.File);
                logger.Info("Running from file!");
                log.Debug($"Param: file={filePath}");

                if (!File.Exists(filePath))
                {
                    log.Error($"File not found at: {filePath}");
                    return;
                }

                var commands = File.ReadAllLines(filePath);
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
                var filePath = PathNormalizer.Normalize(options.File);
                var outputpath = PathNormalizer.Normalize(options.OutputPath);
                logger.Info("Copying a file!");
                log.Debug($"Param: file={filePath}");
                log.Debug($"Param: outputpath={outputpath}");
                log.Debug($"Param: overwrite={options.Overwrite}");

                var source = new FileInfo(filePath);
                if (!source.Exists)
                {
                    log.Error($"File not found at: {filePath}");
                    return;
                }
                if (!Directory.Exists(outputpath))
                {
                    log.Error($"Directory not found at: {outputpath}");
                    return;
                }
                var destination = Path.Combine(outputpath, source.Name);
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
                var startingpath = PathNormalizer.Normalize(options.StartingPath);
                var outputpath = PathNormalizer.Normalize(options.OutputPath);
                logger.Info("Inspecting!");
                log.Debug($"Param: ruleset={options.RuleSet}");
                log.Debug($"Param: startingpath={startingpath}");
                log.Debug($"Param: rule={options.Rule}");
                log.Debug($"Param: outputpath={outputpath}");
                log.Debug($"Param: tokens={options.Tokens}");
                log.Debug($"Param: listtokens={options.ListTokens}");
                log.Debug($"Param: skipxmlvalidation={options.SkipXMLValidation}");

                var i = new Inspector(options.RuleSet,
                                      startingpath,
                                      options.Rule,
                                      outputpath,
                                      TokenManager.IEnumerableToDictionary(options.Tokens),
                                      options.ListTokens,
                                      options.SkipXMLValidation,
                                      log);
                i.Inspect();
            }

            void RunForge(ForgeOptions options)
            {
                var blueprint = PathNormalizer.Normalize(options.Blueprint);
                var outputpath = PathNormalizer.Normalize(options.OutputPath);
                var file = PathNormalizer.Normalize(options.File);
                log.Info("Forging!");
                log.Debug($"Param: blueprint={blueprint}");
                log.Debug($"Param: outputpath={outputpath}");
                log.Debug($"Param: tokens={options.Tokens}");
                log.Debug($"Param: listtokens={options.ListTokens}");
                log.Debug($"Param: skipxmlformattingfix={options.SkipXMLFormattingFix}");
                log.Debug($"Param: skipxmlvalidation={options.SkipXMLValidation}");
                log.Debug($"Param: file={file}");

                var f = new Forger(blueprint,
                                   outputpath,
                                   TokenManager.IEnumerableToDictionary(options.Tokens),
                                   options.ListTokens,
                                   options.SkipXMLFormattingFix,
                                   options.SkipXMLValidation,
                                   log);
                f.Forge(file);
            }

            void RunRoundUp(RoundUpOptions options)
            {
                var map = PathNormalizer.Normalize(options.Map);
                var outputpath = PathNormalizer.Normalize(options.OutputPath);
                var startpath = PathNormalizer.Normalize(options.StartPath);
                var outputfilename = PathNormalizer.Normalize(options.OutputFilename);
                var joinedfilename = PathNormalizer.Normalize(options.JoinedFilename);
                log.Info("Rounding Up!");
                log.Debug($"Param: map={map}");
                log.Debug($"Param: outputpath={outputpath}");
                log.Debug($"Param: outputfilename={outputfilename}");
                log.Debug($"Param: startpath={startpath}");
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
                log.Debug($"Param: joinedfilename={joinedfilename}");

                var w = new Wrangler(map,
                                     outputpath,
                                     outputfilename,
                                     startpath,
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
                                     joinedfilename,
                                     log);
                w.RoundUp();
            }
        }

    }
}
