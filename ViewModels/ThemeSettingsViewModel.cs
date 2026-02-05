using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenRdpGuard.Services;

namespace OpenRdpGuard.ViewModels
{
    public partial class ThemeSettingsViewModel : ObservableObject
    {
        private readonly IAppSettingsService _settingsService;
        private readonly IThemeService _themeService;

        [ObservableProperty]
        private string _currentThemeText = "浅色";

        public ThemeSettingsViewModel(IAppSettingsService settingsService, IThemeService themeService)
        {
            _settingsService = settingsService;
            _themeService = themeService;
            UpdateText();
        }

        private void UpdateText()
        {
            var theme = _settingsService.GetTheme();
            CurrentThemeText = theme switch
            {
                AppTheme.Dark => "深色",
                AppTheme.System => "跟随系统",
                _ => "浅色"
            };
        }

        [RelayCommand]
        private void SetLight()
        {
            _settingsService.SetTheme(AppTheme.Light);
            _themeService.ApplyTheme(AppTheme.Light);
            UpdateText();
        }

        [RelayCommand]
        private void SetDark()
        {
            _settingsService.SetTheme(AppTheme.Dark);
            _themeService.ApplyTheme(AppTheme.Dark);
            UpdateText();
        }

        [RelayCommand]
        private void SetSystem()
        {
            _settingsService.SetTheme(AppTheme.System);
            _themeService.ApplyTheme(AppTheme.Light);
            UpdateText();
        }
    }
}
