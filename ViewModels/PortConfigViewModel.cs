using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenRdpGuard.Services;
using OpenRdpGuard.Views.Dialogs;
using System.Threading.Tasks;
using System.Windows;

namespace OpenRdpGuard.ViewModels
{
    public partial class PortConfigViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly ISystemService _systemService;
        private readonly IWhitelistService _whitelistService;

        [ObservableProperty]
        private int _currentPort;

        [ObservableProperty]
        private int _newPort;

        [ObservableProperty]
        private bool _restartService = true;

        public PortConfigViewModel(ISettingsService settingsService, ISystemService systemService, IWhitelistService whitelistService)
        {
            _settingsService = settingsService;
            _systemService = systemService;
            _whitelistService = whitelistService;
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
                var dialog = new ConfirmDialog("错误", "端口范围必须在 1024-65535 之间。", showCancel: false);
                dialog.Owner = Application.Current?.MainWindow;
                dialog.ShowDialog();
                return;
            }

            var ok = await _settingsService.SetRdpPortAsync(NewPort);
            if (ok && RestartService)
            {
                await _systemService.RestartRdpServiceAsync();
            }

            if (ok)
            {
                await _whitelistService.ApplyWhitelistRulesAsync();
            }

            CurrentPort = _settingsService.GetRdpPort();
            var doneDialog = new ConfirmDialog("完成", "端口已更新。", showCancel: false);
            doneDialog.Owner = Application.Current?.MainWindow;
            doneDialog.ShowDialog();
        }
    }
}
