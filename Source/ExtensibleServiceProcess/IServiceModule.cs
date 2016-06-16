using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace ExtensibleServiceProcess
{
    /// <summary>
    /// A module representing a service that will exist as part of a service application. 
    /// </summary>
    [InheritedExport]
    public interface IServiceModule
    {
        /// <summary>
        /// Called when the service continues.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task OnContinueAsync();

        /// <summary>
        /// Called when a custom command is encountered.
        /// </summary>
        /// <param name="command">The command code.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task OnCustomCommandAsync(int command);

        /// <summary>
        /// Called when the service is paused.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task OnPauseAsync();

        /// <summary>
        /// Called when service shutdown is encountered.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task OnShutdownAsync();

        /// <summary>
        /// Called when the service starts.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task OnStartAsync(string[] args);

        /// <summary>
        /// Called when the service stops.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task OnStopAsync();
    }
}