using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace ExtensibleServiceProcess
{
    [InheritedExport]
    public interface IServiceModule
    {
        Task OnContinueAsync();

        Task OnCustomCommandAsync(int command);

        Task OnPauseAsync();

        Task OnShutdownAsync();

        Task OnStartAsync(string[] args);

        Task OnStopAsync();
    }
}