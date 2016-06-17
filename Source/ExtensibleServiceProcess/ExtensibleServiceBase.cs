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
    [InheritedExport]
    public abstract class ExtensibleServiceBase : ServiceBase
    {
        private readonly TimeSpan serviceControllerTimeout = TimeSpan.FromSeconds(30);
        private string serviceDescription;
        private string serviceDisplayName;

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
        /// Gets or sets the name of the service.
        /// </summary>
        /// <value>The name of the service.</value>
        /// <exception cref="System.InvalidOperationException">Unable to determine the service application name.</exception>
        public new string ServiceName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(base.ServiceName))
                {
                    return base.ServiceName;
                }

                var serviceNameAttribute = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(ServiceNameAttribute), false).FirstOrDefault() as ServiceNameAttribute;
                if (!string.IsNullOrWhiteSpace(serviceNameAttribute?.Name))
                {
                    return serviceNameAttribute.Name;
                }

                var assemblyTitleAttribute = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false).FirstOrDefault() as AssemblyTitleAttribute;
                if (!string.IsNullOrWhiteSpace(assemblyTitleAttribute?.Title))
                {
                    return assemblyTitleAttribute.Title;
                }

                throw new InvalidOperationException("Unable to determine the service application name.");
            }
            set { base.ServiceName = value; }
        }

        /// <summary>
        /// Gets or sets the service description.
        /// </summary>
        /// <value>The service description.</value>
        /// <exception cref="System.InvalidOperationException">Unable to determine the service application description.</exception>
        protected string ServiceDescription
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(serviceDescription))
                {
                    return serviceDescription;
                }

                var serviceDescriptionAttribute = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(ServiceDescriptionAttribute), false).FirstOrDefault() as ServiceDescriptionAttribute;
                if (!string.IsNullOrWhiteSpace(serviceDescriptionAttribute?.Description))
                {
                    return serviceDescriptionAttribute.Description;
                }

                var assemblyDescriptionAttribute = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false).FirstOrDefault() as AssemblyDescriptionAttribute;
                if (!string.IsNullOrWhiteSpace(assemblyDescriptionAttribute?.Description))
                {
                    return assemblyDescriptionAttribute.Description;
                }

                throw new InvalidOperationException("Unable to determine the service application description.");
            }
            set { serviceDescription = value; }
        }

        /// <summary>
        /// Gets or sets the display name of the service.
        /// </summary>
        /// <value>The display name of the service.</value>
        /// <exception cref="System.InvalidOperationException">Unable to determine the service application display name.</exception>
        protected string ServiceDisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(serviceDescription))
                {
                    return serviceDisplayName;
                }

                var serviceDisplayNameAttribute = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(ServiceDisplayNameAttribute), false).FirstOrDefault() as ServiceDisplayNameAttribute;
                if (!string.IsNullOrWhiteSpace(serviceDisplayNameAttribute?.DisplayName))
                {
                    return serviceDisplayNameAttribute.DisplayName;
                }

                var assemblyTitleAttribute = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false).FirstOrDefault() as AssemblyTitleAttribute;
                if (!string.IsNullOrWhiteSpace(assemblyTitleAttribute?.Title))
                {
                    return assemblyTitleAttribute.Title;
                }

                throw new InvalidOperationException("Unable to determine the service application display name.");
            }
            set { serviceDisplayName = value; }
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
        public void StartService()
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
        public void StopService()
        {
            if (ServiceController.GetServices().FirstOrDefault(s => s.ServiceName.Equals(ServiceName, StringComparison.OrdinalIgnoreCase)) == null) return;

            using (var serviceController = new ServiceController(this.ServiceName))
            {
                if (serviceController.Status == ServiceControllerStatus.StartPending) serviceController.WaitForStatus(ServiceControllerStatus.Running, serviceControllerTimeout);
                if (serviceController.Status == ServiceControllerStatus.Running) serviceController.Stop();
            }
        }

        /// <summary>
        /// Installs the service.
        /// </summary>
        /// <param name="accountType">Type of the account under which to run this service application.</param>
        /// <param name="userName">The user account under which the service application will run.</param>
        /// <param name="password">The password associated with the user account under which the service application will run.</param>
        public void InstallService(ServiceAccount accountType = ServiceAccount.NetworkService, string userName = null, string password = null)
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
        /// Uninstalls the service.
        /// </summary>
        public void UninstallService()
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