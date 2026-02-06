using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenRdpGuard.Services;
using OpenRdpGuard.Views.Dialogs;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace OpenRdpGuard.ViewModels
{
    public partial class RestoreDefaultsViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IWhitelistService _whitelistService;
        private readonly IFirewallService _firewallService;
        private readonly ISystemService _systemService;

        public RestoreDefaultsViewModel(ISettingsService settingsService, IWhitelistService whitelistService, IFirewallService firewallService, ISystemService systemService)
        {
            _settingsService = settingsService;
            _whitelistService = whitelistService;
            _firewallService = firewallService;
            _systemService = systemService;
        }

        [RelayCommand]
        private async Task Restore()
        {
            var confirmDialog = new ConfirmDialog("确认", "确认恢复默认设置？这将重置端口为 3389，关闭白名单并清空黑名单。");
            confirmDialog.Owner = Application.Current?.MainWindow;
            if (confirmDialog.ShowDialog() != true) return;

            await _settingsService.SetRdpPortAsync(3389);
            await _whitelistService.ApplyWhitelistRulesAsync();
            await _systemService.RestartRdpServiceAsync();

            var allowed = await _whitelistService.GetAllowedIpsAsync();
            foreach (var ip in allowed.ToList())
            {
                await _whitelistService.RemoveIpAsync(ip);
            }
            await _whitelistService.SetWhitelistEnabledAsync(false);

            var blocked = await _firewallService.GetBlockedIpsAsync();
            foreach (var ip in blocked.ToList())
            {
                await _firewallService.UnblockIpAsync(ip);
            }

            var doneDialog = new ConfirmDialog("完成", "默认设置已恢复。", showCancel: false);
            doneDialog.Owner = Application.Current?.MainWindow;
            doneDialog.ShowDialog();
        }
    }
}
