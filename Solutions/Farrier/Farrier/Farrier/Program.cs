using System;
using CommandLine;
using NLog;
using Farrier.Helpers;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

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
                    args = "forge".Split();
                }

                Parser.Default.ParseArguments(args, LoadVerbs())
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
            }

            void RunInspect(InspectOptions options)
            {
                logger.Info("Inspecting!");
            }

            void RunForge(ForgeOptions options)
            {
                logger.Info("Forging!");
            }
        }

    }
}
