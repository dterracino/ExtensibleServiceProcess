using System;

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
        }

        internal static int Main(string[] args)
        {
            // See AssemblyInfo.cs for example of how to set service name, display name, and description.
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