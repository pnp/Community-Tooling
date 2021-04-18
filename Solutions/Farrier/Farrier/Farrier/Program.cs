using System;
using CommandLine;
using NLog;
using Farrier.Helpers;

namespace Farrier
{
    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            ExitCode exitcode;
            try
            {
                //TEMPORARY DEFAULT VALUES
                if (System.Diagnostics.Debugger.IsAttached && args.Length == 0)
                {
                    //args = new string[] { "-c", @"Samples/ListFormatting/Playground.xml", "--listtokens" };
                }

                //Verb routing and option parsing
                /*exitcode = CommandLine.Parser.Default.ParseArguments<FarrierOptions>(args)
                    .MapResult(
                        (FarrierOptions opts) => { return PerformOperation(opts); },
                        _ => { return ExitCode.Failure; });*/
                exitcode = PerformOperation();
            }
            catch (Exception e)
            {
                logger.Error(e, e.Message + e.StackTrace);
                exitcode = ExitCode.Failure;
            }

            Environment.Exit((int)exitcode);
        }

        public static ExitCode PerformOperation()
        {
            return ExitCode.Success;
        }
    }
}
