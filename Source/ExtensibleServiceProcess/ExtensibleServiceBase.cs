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
    /// <summary>
    /// Provides a base class for an extensible service that will exist as part of a service application.
    /// </summary>
    public abstract class ExtensibleServiceBase : ServiceBase
    {
        private readonly TimeSpan serviceControllerTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets a value indicating whether multiple service starts are allowed.
        /// </summary>
        /// <value><c>true</c> if [allow multiple service starts]; otherwise, <c>false</c>.</value>
        protected bool AllowMultipleServiceStarts { get; set; }

        /// <summary>
        /// Gets the service modules.
        /// </summary>
        /// <value>The modules.</value>
        protected IEnumerable<IServiceModule> Modules { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is a new instance.
        /// </summary>
        /// <value><c>true</c> if this instance is a new instance; otherwise, <c>false</c>.</value>
        public bool IsNewInstance => Instance.IsNew($"{ServiceName}.Start");

        /// <summary>
        /// Gets or sets the service description.
        /// </summary>
        /// <value>The service description.</value>
        protected string ServiceDescription { get; set; }

        /// <summary>
        /// Gets or sets the display name of the service.
        /// </summary>
        /// <value>The display name of the service.</value>
        protected string ServiceDisplayName { get; set; }

        /// <summary>
        /// Installs the service.
        /// </summary>
        /// <param name="accountType">Type of the account under which to run this service application.</param>
        /// <param name="userName">The user account under which the service application will run.</param>
        /// <param name="password">The password associated with the user account under which the service application will run.</param>
        protected void InstallService(ServiceAccount accountType = ServiceAccount.NetworkService, string userName = null, string password = null)
        {
            if (ServiceController.GetServices().FirstOrDefault(s => s.ServiceName.Equals(ServiceName, StringComparison.OrdinalIgnoreCase)) != null) return;

            using (var processInstaller = new ServiceProcessInstaller())
            {
                processInstaller.Account = accountType;
                processInstaller.Username = userName;
                processInstaller.Password = password;

                using (var serviceInstaller = new ServiceInstaller { Parent = processInstaller })
                {
                    serviceInstaller.StartType = ServiceStartMode.Automatic;
                    serviceInstaller.ServiceName = ServiceName;
                    serviceInstaller.DisplayName = ServiceDisplayName;
                    serviceInstaller.Description = ServiceDescription;

                    string[] commandLine = { $"/assemblypath={Assembly.GetEntryAssembly().Location}" };
                    serviceInstaller.Context = new InstallContext(null, commandLine);

                    var state = new ListDictionary();
                    serviceInstaller.Install(state);
                }
            }
        }

        /// <summary>
        /// Called when the service continues.
        /// </summary>
        protected override void OnContinue()
        {
            Modules?.AsParallel().ForAll(w => w.OnContinueAsync());
        }

        /// <summary>
        /// Called when a custom command is encountered.
        /// </summary>
        /// <param name="command">The command code.</param>
        protected override void OnCustomCommand(int command)
        {
            Modules?.AsParallel().ForAll(w => w.OnCustomCommandAsync(command));
        }

        /// <summary>
        /// Called when the service is paused.
        /// </summary>
        protected override void OnPause()
        {
            Modules?.AsParallel().ForAll(w => w.OnPauseAsync());
        }

        /// <summary>
        /// Called when service shutdown is encountered..
        /// </summary>
        protected override void OnShutdown()
        {
            Modules?.AsParallel().ForAll(w => w.OnShutdownAsync());
        }

        /// <summary>
        /// Called when the service starts.
        /// </summary>
        /// <param name="args">The arguments.</param>
        protected override void OnStart(string[] args)
        {
            if (!AllowMultipleServiceStarts && !IsNewInstance) return;

            Modules = null;

            var assemblyFileLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyFileInfo = new FileInfo(assemblyFileLocation);
            var path = assemblyFileInfo.Directory?.FullName;

            if (path == null) return;

            var executableDirectory = new DirectoryInfo(path);
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

        /// <summary>
        /// Called when the service stops.
        /// </summary>
        protected override void OnStop()
        {
            Modules?.AsParallel().ForAll(w => w.OnStopAsync());
        }

        /// <summary>
        /// Starts the service.
        /// </summary>
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

        /// <summary>
        /// Stops the service.
        /// </summary>
        protected void StopService()
        {
            if (ServiceController.GetServices().FirstOrDefault(s => s.ServiceName.Equals(ServiceName, StringComparison.OrdinalIgnoreCase)) == null) return;

            using (var serviceController = new ServiceController(this.ServiceName))
            {
                if (serviceController.Status == ServiceControllerStatus.StartPending) serviceController.WaitForStatus(ServiceControllerStatus.Running, serviceControllerTimeout);
                if (serviceController.Status == ServiceControllerStatus.Running) serviceController.Stop();
            }
        }

        /// <summary>
        /// Uninstalls the service.
        /// </summary>
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