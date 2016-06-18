# ExtensibleServiceProcess

[![Build](https://ci.appveyor.com/api/projects/status/t70sgur26vw0s86p?svg=true)](https://ci.appveyor.com/project/skthomasjr/extensibleserviceprocess)
[![Release](https://img.shields.io/github/release/skthomasjr/ExtensibleServiceProcess.svg?maxAge=2592000)](https://github.com/skthomasjr/ExtensibleServiceProcess/releases)
[![NuGet](https://img.shields.io/nuget/v/ExtensibleServiceProcess.svg)](https://www.nuget.org/packages/ExtensibleServiceProcess)
[![License](https://img.shields.io/github/license/skthomasjr/ExtensibleServiceProcess.svg?maxAge=2592000)](LICENSE.md)
[![Author](https://img.shields.io/badge/author-Scott%20K.%20Thomas%2C%20Jr.-blue.svg?maxAge=2592000)](https://www.linkedin.com/in/skthomasjr)
[![Join the chat at https://gitter.im/skthomasjr/ExtensibleServiceProcess](https://badges.gitter.im/skthomasjr/ExtensibleServiceProcess.svg)](https://gitter.im/skthomasjr/ExtensibleServiceProcess?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

ExtensibleServiceProcess is a library to support an extensible service application. Deriving from ExtensibleServiceBase creates a new Windows service class that runs as a Windows service. The process context can easily switch from running as a Windows service to running as a console application (or any other .NET application). The following example shows a Windows service/console application:

```C#
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
    using (var service = new Service())
    {
      if (Environment.UserInteractive)
      {
        service.OnStart(args);

        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
  
        service.OnStop();
      }
      else Run(service);
    }
    return 0;
  }
}
```
The Windows service can be installed or uninstalled from the service instance with the following code:
```c#
service.InstallService();

// or

service.UninstallService();
```
The Windows service can be started or stopped from the service instance with the following code:
```c#
service.StartService();

// or

service.StopService();
```
Attributes can be added to the AsemblyInfo.cs to add Windows service specific metadata required for service control operations.
```c#
[assembly: ServiceName("SampleService")]
[assembly: ServiceDisplayName("Sample Service")]
[assembly: ServiceDescription("This is a SampleService.")]
```
To create an injectable Windows service module implement IServiceModule.
```c#
public class SampleServiceModule : IServiceModule
{
  public async Task OnContinueAsync()
  {
    ...
  }
  
  public async Task OnCustomCommandAsync(int command)
  {
    ...
  }
  
  public async Task OnPauseAsync()
  {
    ...
  }
  
  public async Task OnShutdownAsync()
  {
    ...
  }
  
  public async Task OnStartAsync(string[] args)
  {
    ...
  }
  
  public async Task OnStopAsync()
  {
    ...
  }
}
```
