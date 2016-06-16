using System;
using System.Reflection;

namespace ExtensibleServiceProcess.SampleConsole
{
    internal class Service : ExtensibleServiceBase
    {
        private Service()
        {
            AllowMultipleServiceStarts = false;
            AutoLog = true;
            CanHandlePowerEvent = false;
            CanHandleSessionChangeEvent = false;
            CanPauseAndContinue = false;
            CanShutdown = false;
            CanStop = true;
            ExitCode = 0;
            ServiceName = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title;
            ServiceDescription = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
            ServiceDisplayName = "Sample Service";
        }

        internal static int Main(string[] args)
        {
            using (var service = new Service())
            {
                Console.Title = "Sample Console";
                Console.CursorVisible = false;

                if (Environment.UserInteractive)
                {
                    service.OnStart(args);
                    service.OnStop();
                }
                else
                {
                    Run(service);
                }

                Console.WriteLine();
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
            return 0;
        }
    }
}