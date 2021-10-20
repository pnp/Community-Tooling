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
                    args = @"forge -b Samples/ListFormatting/Playground.xml --listtokens".Split();
                    //args = "inspect -c lfsample.xml".Split();
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
                logger.Info("Forging!");
                logger.Debug("Param: blueprint={0}", options.Blueprint);
                logger.Debug("Param: outputpath={0}", options.OutputPath);
                logger.Debug("Param: tokens={0}", options.Tokens);
                logger.Debug("Param: listtokens={0}", options.ListTokens);
                logger.Debug("Param: skipxmlformattingfix={0}", options.SkipXMLFormattingFix);

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
                logger.Info("Rounding Up!");
                logger.Debug("Param: map={0}", options.Map);
                logger.Debug("Param: outputpath={0}", options.OutputPath);
                logger.Debug("Param: jsonpath={0}", options.JSONPath);

                var w = new Wrangler(options.Map,
                                     options.OutputPath,
                                     options.JSONPath,
                                     log);
                w.RoundUp();
            }
        }

    }
}
