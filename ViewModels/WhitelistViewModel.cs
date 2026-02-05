using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenRdpGuard.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace OpenRdpGuard.ViewModels
{
    public partial class WhitelistViewModel : ObservableObject
    {
        private readonly IWhitelistService _whitelistService;

        [ObservableProperty]
        private ObservableCollection<string> _allowedIps = new();

        [ObservableProperty]
        private string _ipToAdd = string.Empty;

        [ObservableProperty]
        private bool _isWhitelistEnabled;

        public WhitelistViewModel(IWhitelistService whitelistService)
        {
            _whitelistService = whitelistService;
            LoadData();
        }

        private async void LoadData()
        {
            var ips = await _whitelistService.GetAllowedIpsAsync();
            AllowedIps = new ObservableCollection<string>(ips);
            IsWhitelistEnabled = _whitelistService.IsWhitelistEnabled();
        }

        [RelayCommand]
        private async Task AddIp()
        {
            if (!string.IsNullOrWhiteSpace(IpToAdd))
            {
                await _whitelistService.AllowIpAsync(IpToAdd.Trim());
                IpToAdd = string.Empty;
                LoadData();
            }
        }

        [RelayCommand]
        private async Task RemoveIp(string ip)
        {
            if (!string.IsNullOrWhiteSpace(ip))
            {
                await _whitelistService.RemoveIpAsync(ip);
                LoadData();
            }
        }

        [RelayCommand]
        private async Task ToggleWhitelist()
        {
            if (IsWhitelistEnabled && AllowedIps.Count == 0)
            {
                MessageBox.Show("白名单为空，无法启用。请先添加 IP。", "提示");
                IsWhitelistEnabled = false;
                return;
            }

            await _whitelistService.SetWhitelistEnabledAsync(IsWhitelistEnabled);
        }
    }
}
