using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenRdpGuard.Services;
using System.Threading.Tasks;
using System.Windows;

namespace OpenRdpGuard.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly ISystemService _systemService;

        [ObservableProperty]
        private int _rdpPort = 3389;

        [ObservableProperty]
        private string _appVersion = "1.0.0";

        public SettingsViewModel(ISettingsService settingsService, ISystemService systemService)
        {
            _settingsService = settingsService;
            _systemService = systemService;
            LoadData();
        }

        private void LoadData()
        {
            RdpPort = _settingsService.GetRdpPort();
        }

        [RelayCommand]
        private async Task SavePort()
        {
            if (RdpPort > 0 && RdpPort < 65536)
            {
                await _settingsService.SetRdpPortAsync(RdpPort);
                MessageBox.Show($"RDP Port changed to {RdpPort}. You may need to restart the RDP service.", "Success");
            }
            else
            {
                MessageBox.Show("Invalid Port Number", "Error");
            }
        }

        [RelayCommand]
        private async Task RestartRdp()
        {
            await _systemService.RestartRdpServiceAsync();
            MessageBox.Show("RDP Service Restarted", "Info");
        }
    }
}
