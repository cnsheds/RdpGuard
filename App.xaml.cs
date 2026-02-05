using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenRdpGuard.ViewModels;
using OpenRdpGuard.Views;
using OpenRdpGuard.Views.Pages;
using System;
using System.Windows;
using OpenRdpGuard.Services;

namespace OpenRdpGuard
{
    public partial class App : Application
    {
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IAppSettingsService, AppSettingsService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ILogService, LogService>();
                services.AddSingleton<IFirewallService, FirewallService>();
                services.AddSingleton<ISystemService, SystemService>();
                services.AddSingleton<IConnectionService, ConnectionService>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IWhitelistService, WhitelistService>();

                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();

                services.AddSingleton<LoginStatsPage>();
                services.AddSingleton<LoginStatsViewModel>();

                services.AddSingleton<BlacklistPage>();
                services.AddSingleton<BlacklistViewModel>();

                services.AddSingleton<WhitelistPage>();
                services.AddSingleton<WhitelistViewModel>();

                services.AddSingleton<ConnectionsPage>();
                services.AddSingleton<ConnectionsViewModel>();

                services.AddSingleton<UsersPage>();
                services.AddSingleton<UsersViewModel>();

                services.AddSingleton<PortConfigPage>();
                services.AddSingleton<PortConfigViewModel>();

                services.AddSingleton<RestoreDefaultsPage>();
                services.AddSingleton<RestoreDefaultsViewModel>();

                services.AddSingleton<ThemeSettingsPage>();
                services.AddSingleton<ThemeSettingsViewModel>();
            })
            .Build();

        public static IServiceProvider Services => _host.Services;

        protected override async void OnStartup(StartupEventArgs e)
        {
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

            await _host.StartAsync();

            ApplyTheme();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private void ApplyTheme()
        {
            var settings = _host.Services.GetRequiredService<IAppSettingsService>();
            var themeService = _host.Services.GetRequiredService<IThemeService>();
            var theme = settings.GetTheme();
            var effectiveTheme = theme == AppTheme.System ? AppTheme.Light : theme;
            themeService.ApplyTheme(effectiveTheme);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
            base.OnExit(e);
        }
    }
}
