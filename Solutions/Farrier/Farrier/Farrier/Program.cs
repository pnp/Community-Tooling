using System;
using CommandLine;
using NLog;
using Farrier.Helpers;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Farrier.Parser;
using Farrier.Forge;
using Farrier.RoundUp;

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
                    //args = "inspect -c lfsample.xml".Split();
                    args = @"roundup -m Samples/ListFormatting/LFAssetMap.xml -s D:\Code\PnP\sp-dev-list-formatting -j sample.json --overwrite".Split();
                    //args = @"roundup -m Samples/ListFormatting/LFAssetMap.xml".Split();
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
            }

            void RunInspect(InspectOptions options)
            {
                logger.Info("Inspecting!");
            }

            void RunForge(ForgeOptions options)
            {
                log.Info("Forging!");
                log.Debug($"Param: blueprint={options.Blueprint}");
                log.Debug($"Param: outputpath={options.OutputPath}");
                log.Debug($"Param: tokens={options.Tokens}");
                log.Debug($"Param: listtokens={options.ListTokens}");
                log.Debug($"Param: skipxmlformattingfix={options.SkipXMLFormattingFix}");

                var f = new Forger(options.Blueprint,
                                   options.OutputPath,
                                   TokenManager.IEnumerableToDictionary(options.Tokens),
                                   options.ListTokens,
                                   options.SkipXMLFormattingFix, 
                                   log);
                f.Forge();
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
                                     log);
                w.RoundUp();
            }
        }

    }
}
