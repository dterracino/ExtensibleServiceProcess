using System.Threading.Tasks;

namespace ExtensibleServiceProcess.SampleConsole
{
    public class SampleServiceModule : IServiceModule
    {
        public async Task OnContinueAsync()
        {
            await Task.FromResult(false);
        }

        public async Task OnCustomCommandAsync(int command)
        {
            await Task.FromResult(false);
        }

        public async Task OnPauseAsync()
        {
            await Task.FromResult(false);
        }

        public async Task OnShutdownAsync()
        {
            await Task.FromResult(false);
        }

        public async Task OnStartAsync(string[] args)
        {
            await Task.FromResult(false);
        }

        public async Task OnStopAsync()
        {
            await Task.FromResult(false);
        }
    }
}