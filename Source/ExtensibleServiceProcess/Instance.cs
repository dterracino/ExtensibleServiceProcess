using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace ExtensibleServiceProcess
{
    public static class Instance
    {
        private static Semaphore instanceSemaphore;

        public static bool IsNew(string semaphoreName)
        {
            bool isNewInstance;

            var semaphoreSecurity = new SemaphoreSecurity();
            var securityIdentifier = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var semaphoreAccessRule = new SemaphoreAccessRule(securityIdentifier, SemaphoreRights.FullControl, AccessControlType.Allow);

            semaphoreSecurity.AddAccessRule(semaphoreAccessRule);
            instanceSemaphore = new Semaphore(0, 1, $"Global\\{semaphoreName}", out isNewInstance, semaphoreSecurity);

            return isNewInstance;
        }
    }
}