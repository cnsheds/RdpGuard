using System.ServiceProcess;
using System.Management;
using System.Threading.Tasks;

namespace OpenRdpGuard.Services
{
    public interface ISystemService
    {
        bool IsRdpServiceRunning();
        bool IsFirewallServiceRunning();
        DateTime? GetServerStartupTime();
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

        public DateTime? GetServerStartupTime()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem");
                using var results = searcher.Get();
                foreach (ManagementObject os in results)
                {
                    var bootTimeValue = os["LastBootUpTime"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(bootTimeValue))
                    {
                        return ManagementDateTimeConverter.ToDateTime(bootTimeValue);
                    }
                }
            }
            catch
            {
            }

            return null;
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
