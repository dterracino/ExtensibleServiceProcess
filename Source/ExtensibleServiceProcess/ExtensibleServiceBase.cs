using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;

namespace ExtensibleServiceProcess
{
    public abstract class ExtensibleServiceBase : ServiceBase
    {
        private readonly TimeSpan serviceControllerTimeout = TimeSpan.FromSeconds(15);

        protected bool AllowMultipleServiceStarts { get; set; }

        protected IEnumerable<IServiceModule> Modules { get; private set; }

        protected string ServiceDescription { get; set; }

        protected string ServiceDisplayName { get; set; }

        protected void InstallService(ServiceAccount accountType = ServiceAccount.NetworkService, string userName = null, string password = null)
        {
            if (ServiceController.GetServices().FirstOrDefault(s => s.ServiceName.Equals(this.ServiceName, StringComparison.OrdinalIgnoreCase)) != null) return;

            using (var processInstaller = new ServiceProcessInstaller())
            {
                processInstaller.Account = accountType;
                processInstaller.Username = userName;
                processInstaller.Password = password;

                using (var serviceInstaller = new ServiceInstaller { Parent = processInstaller })
                {
                    serviceInstaller.StartType = ServiceStartMode.Automatic;
                    serviceInstaller.ServiceName = this.ServiceName;
                    serviceInstaller.DisplayName = ServiceDisplayName;
                    serviceInstaller.Description = ServiceDescription;

                    string[] commandline = { $"/assemblypath={Assembly.GetEntryAssembly().Location}" };
                    serviceInstaller.Context = new InstallContext(null, commandline);

                    var state = new ListDictionary();
                    serviceInstaller.Install(state);
                }
            }
        }

        protected override void OnContinue()
        {
            Modules?.AsParallel().ForAll(w => w.OnContinueAsync());
        }

        protected override void OnCustomCommand(int command)
        {
            Modules?.AsParallel().ForAll(w => w.OnCustomCommandAsync(command));
        }

        protected override void OnPause()
        {
            Modules?.AsParallel().ForAll(w => w.OnPauseAsync());
        }

        protected override void OnShutdown()
        {
            Modules?.AsParallel().ForAll(w => w.OnShutdownAsync());
        }

        protected override void OnStart(string[] args)
        {
            if (!AllowMultipleServiceStarts && !Instance.IsNew($"{ServiceName}.Start")) return;

            Modules = null;

            var executableDirectory = new DirectoryInfo(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName);
            var moduleDirectory = new DirectoryInfo(Path.Combine(executableDirectory.FullName, "Modules"));

            using (var catalog = new AggregateCatalog(new DirectoryCatalog(executableDirectory.FullName)))
            {
                if (moduleDirectory.Exists)
                {
                    catalog.Catalogs.Add(new DirectoryCatalog(moduleDirectory.FullName));
                }

                using (var container = new CompositionContainer(catalog))
                {
                    container.ComposeParts(this);
                    Modules = container.GetExportedValues<IServiceModule>();
                }
            }

            Modules?.AsParallel().ForAll(w => w.OnStartAsync(args));
        }

        protected override void OnStop()
        {
            Modules?.AsParallel().ForAll(w => w.OnStopAsync());
        }

        protected void StartService()
        {
            if (ServiceController.GetServices().FirstOrDefault(s => s.ServiceName.Equals(ServiceName, StringComparison.OrdinalIgnoreCase)) == null) return;

            using (var serviceController = new ServiceController(this.ServiceName))
            {
                if (serviceController.Status == ServiceControllerStatus.PausePending) serviceController.WaitForStatus(ServiceControllerStatus.Paused, serviceControllerTimeout);
                if (serviceController.Status == ServiceControllerStatus.Paused) serviceController.Continue();

                if (serviceController.Status == ServiceControllerStatus.StopPending) serviceController.WaitForStatus(ServiceControllerStatus.Stopped, serviceControllerTimeout);
                if (serviceController.Status == ServiceControllerStatus.Stopped) serviceController.Start();
            }
        }

        protected void StopService()
        {
            if (ServiceController.GetServices().FirstOrDefault(s => s.ServiceName.Equals(ServiceName, StringComparison.OrdinalIgnoreCase)) == null) return;

            using (var serviceController = new ServiceController(this.ServiceName))
            {
                if (serviceController.Status == ServiceControllerStatus.StartPending) serviceController.WaitForStatus(ServiceControllerStatus.Running, serviceControllerTimeout);
                if (serviceController.Status == ServiceControllerStatus.Running) serviceController.Stop();
            }
        }

        protected void UninstallService()
        {
            if (ServiceController.GetServices().FirstOrDefault(s => s.ServiceName.Equals(ServiceName, StringComparison.OrdinalIgnoreCase)) == null) return;

            using (var serviceInstaller = new ServiceInstaller { ServiceName = ServiceName })
            {
                using (var serviceController = new ServiceController(serviceInstaller.ServiceName))
                {
                    if (serviceController.Status == ServiceControllerStatus.StartPending) serviceController.WaitForStatus(ServiceControllerStatus.Running, serviceControllerTimeout);
                    if (serviceController.Status == ServiceControllerStatus.PausePending) serviceController.WaitForStatus(ServiceControllerStatus.Paused, serviceControllerTimeout);

                    if ((serviceController.Status == ServiceControllerStatus.Running) || (serviceController.Status == ServiceControllerStatus.Paused))
                    {
                        serviceController.Stop();
                        serviceController.WaitForStatus(ServiceControllerStatus.Stopped, serviceControllerTimeout);
                        serviceController.Close();
                    }
                }
                serviceInstaller.Context = new InstallContext();
                serviceInstaller.Uninstall(null);
            }
        }
    }
}