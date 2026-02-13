using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenRdpGuard.Models;
using OpenRdpGuard.Services;
using OpenRdpGuard.Views.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace OpenRdpGuard.ViewModels
{
    public partial class ShareManageViewModel : ObservableObject
    {
        private readonly IShareService _shareService;

        [ObservableProperty]
        private ObservableCollection<ShareEntry> _shares = new();

        [ObservableProperty]
        private ShareEntry? _selectedShare;

        [ObservableProperty]
        private bool _autoAdminShareEnabled = true;

        [ObservableProperty]
        private string _statusMessage = "未加载";

        [ObservableProperty]
        private bool _isBusy;

        public ShareManageViewModel(IShareService shareService)
        {
            _shareService = shareService;
            _ = Refresh();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var list = await _shareService.GetSharesAsync();
                Shares = new ObservableCollection<ShareEntry>(list);
                AutoAdminShareEnabled = await _shareService.GetAutoAdminShareEnabledAsync();
                StatusMessage = $"已加载 {Shares.Count} 个共享";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败：{ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteSelected()
        {
            if (SelectedShare == null)
            {
                return;
            }

            var target = SelectedShare;
            var warning = target.IsSystemShare
                ? "IPC$ 可能影响远程管理，确定删除吗？"
                : target.IsAdminShare
                    ? $"{target.Name} 属于默认管理共享，删除后可能影响运维工具，确定继续吗？"
                    : $"确定删除共享 {target.Name} 吗？";

            var dialog = new ConfirmDialog("确认", warning);
            dialog.Owner = Application.Current?.MainWindow;
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var result = await _shareService.DeleteShareAsync(target.Name);
            if (!result.Success)
            {
                var failDialog = new ConfirmDialog("错误", result.Message, showCancel: false);
                failDialog.Owner = Application.Current?.MainWindow;
                failDialog.ShowDialog();
                return;
            }

            StatusMessage = result.Message;
            await Refresh();
        }

        [RelayCommand]
        private async Task ApplyAutoAdminSharePolicy()
        {
            var result = await _shareService.SetAutoAdminShareEnabledAsync(AutoAdminShareEnabled);
            if (!result.Success)
            {
                var failDialog = new ConfirmDialog("错误", result.Message, showCancel: false);
                failDialog.Owner = Application.Current?.MainWindow;
                failDialog.ShowDialog();
                return;
            }

            var okDialog = new ConfirmDialog("完成", result.Message, showCancel: false);
            okDialog.Owner = Application.Current?.MainWindow;
            okDialog.ShowDialog();
            StatusMessage = result.Message;
            await Refresh();
        }
    }
}
