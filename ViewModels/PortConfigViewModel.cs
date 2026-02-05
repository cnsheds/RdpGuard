using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenRdpGuard.Services;
using System.Threading.Tasks;
using System.Windows;

namespace OpenRdpGuard.ViewModels
{
    public partial class PortConfigViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly ISystemService _systemService;

        [ObservableProperty]
        private int _currentPort;

        [ObservableProperty]
        private int _newPort;

        [ObservableProperty]
        private bool _restartService = true;

        public PortConfigViewModel(ISettingsService settingsService, ISystemService systemService)
        {
            _settingsService = settingsService;
            _systemService = systemService;
            Load();
        }

        private void Load()
        {
            CurrentPort = _settingsService.GetRdpPort();
            NewPort = CurrentPort;
        }

        [RelayCommand]
        private async Task ApplyPort()
        {
            if (NewPort < 1024 || NewPort > 65535)
            {
                MessageBox.Show("端口范围必须在 1024-65535 之间。", "错误");
                return;
            }

            var ok = await _settingsService.SetRdpPortAsync(NewPort);
            if (ok && RestartService)
            {
                await _systemService.RestartRdpServiceAsync();
            }

            CurrentPort = _settingsService.GetRdpPort();
            MessageBox.Show("端口已更新。", "完成");
        }
    }
}
