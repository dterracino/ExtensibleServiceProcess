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

            var assemblyTitle = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title;
            var assemblyDescription = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>().Description;

            ServiceName = assemblyTitle;
            ServiceDisplayName = assemblyTitle;
            ServiceDescription = assemblyDescription;
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

                    Console.WriteLine();
                    Console.WriteLine("This application will run as a console application and a service application.");
                    Console.WriteLine();
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey();

                    service.OnStop();
                }
                else
                {
                    Run(service);
                }
            }
            return 0;
        }
    }
}