using System.ServiceProcess;
using System.Threading.Tasks;

namespace OpenRdpGuard.Services
{
    public interface ISystemService
    {
        bool IsRdpServiceRunning();
        bool IsFirewallServiceRunning();
        Task RestartRdpServiceAsync();
    }

    public class SystemService : ISystemService
    {
        public bool IsRdpServiceRunning()
        {
            return IsServiceRunning("TermService");
        }

        public bool IsFirewallServiceRunning()
        {
            return IsServiceRunning("MpsSvc");
        }

        private bool IsServiceRunning(string serviceName)
        {
            try
            {
                using var sc = new ServiceController(serviceName);
                return sc.Status == ServiceControllerStatus.Running;
            }
            catch
            {
                return false;
            }
        }

        public async Task RestartRdpServiceAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    using var sc = new ServiceController("TermService");
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped);
                    }
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running);
                }
                catch { }
            });
        }
    }
}
