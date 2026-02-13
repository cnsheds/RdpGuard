using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenRdpGuard.ViewModels;
using OpenRdpGuard.Views;
using OpenRdpGuard.Views.Pages;
using System;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using OpenRdpGuard.Services;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Application = System.Windows.Application;

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
                services.AddSingleton<IShareService, ShareService>();

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

                services.AddSingleton<ShareManagePage>();
                services.AddSingleton<ShareManageViewModel>();

                services.AddSingleton<PortConfigPage>();
                services.AddSingleton<PortConfigViewModel>();

                services.AddSingleton<RestoreDefaultsPage>();
                services.AddSingleton<RestoreDefaultsViewModel>();

                services.AddSingleton<ThemeSettingsPage>();
                services.AddSingleton<ThemeSettingsViewModel>();
            })
            .Build();

        public static IServiceProvider Services => _host.Services;
        private TaskbarIcon? _trayIcon;
        private bool _allowExit;
        private MainWindow? _mainWindow;
        private Mutex? _singleInstanceMutex;
        private CancellationTokenSource? _pipeCts;
        private Task? _pipeListenerTask;
        private const string SingleInstanceMutexName = "RdpGuard.SingleInstance";
        private const string SingleInstancePipeName = "RdpGuard.SingleInstance.Pipe";

        protected override async void OnStartup(StartupEventArgs e)
        {
            var createdNew = false;
            _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out createdNew);
            if (!createdNew)
            {
                await NotifyExistingInstanceAsync();
                Shutdown();
                return;
            }

            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

            await _host.StartAsync();

            ApplyTheme();

            _mainWindow = _host.Services.GetRequiredService<MainWindow>();
            _mainWindow.Show();
            InitializeTray(_mainWindow);
            StartPipeListener();
            _ = _host.Services.GetRequiredService<BlacklistViewModel>();

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
            if (_trayIcon != null)
            {
                _trayIcon.Dispose();
            }
            await StopPipeListenerAsync();
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
            await _host.StopAsync();
            _host.Dispose();
            base.OnExit(e);
        }

        private void InitializeTray(MainWindow mainWindow)
        {
            var appIcon = GetApplicationIcon() ?? SystemIcons.Application;
            _trayIcon = new TaskbarIcon
            {
                Icon = appIcon,
                ToolTipText = "RdpGuard"
            };

            var menu = new ContextMenu();
            var exitIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M3,3 L12,12 M12,3 L3,12"),
                Stroke = System.Windows.Media.Brushes.DarkRed,
                StrokeThickness = 2,
                Width = 10,
                Height = 10,
                Stretch = Stretch.Uniform
            };
            var exitItem = new MenuItem { Header = "退出", Icon = exitIcon };
            exitItem.Click += (_, __) => ExitFromTray();
            menu.Items.Add(exitItem);
            _trayIcon.ContextMenu = menu;

            _trayIcon.TrayLeftMouseDown += (_, __) => RestoreFromTray();

            mainWindow.Closing += (_, args) =>
            {
                if (_allowExit) return;
                args.Cancel = true;
                mainWindow.Hide();
            };
        }

        private static Icon? GetApplicationIcon()
        {
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(exePath))
                {
                    return Icon.ExtractAssociatedIcon(exePath);
                }
            }
            catch
            {
            }

            return null;
        }

        private void RestoreFromTray()
        {
            if (_mainWindow == null) return;
            _mainWindow.Show();
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
            _mainWindow.Activate();
            _mainWindow.Topmost = true;
            _mainWindow.Topmost = false;
            _mainWindow.Focus();
        }

        private void ExitFromTray()
        {
            _allowExit = true;
            if (_mainWindow != null)
            {
                _mainWindow.Close();
            }
            Shutdown();
        }

        private void StartPipeListener()
        {
            _pipeCts = new CancellationTokenSource();
            _pipeListenerTask = Task.Run(() => ListenPipeAsync(_pipeCts.Token));
        }

        private async Task StopPipeListenerAsync()
        {
            if (_pipeCts != null)
            {
                _pipeCts.Cancel();
            }
            if (_pipeListenerTask != null)
            {
                try { await _pipeListenerTask; } catch { }
            }
        }

        private async Task ListenPipeAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(SingleInstancePipeName, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                    await server.WaitForConnectionAsync(token);
                    using var reader = new StreamReader(server);
                    var message = await reader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        _ = Dispatcher.InvokeAsync(RestoreFromTray);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    await Task.Delay(200, token);
                }
            }
        }

        private static async Task NotifyExistingInstanceAsync()
        {
            try
            {
                using var client = new NamedPipeClientStream(".", SingleInstancePipeName, PipeDirection.Out);
                await client.ConnectAsync(200);
                using var writer = new StreamWriter(client) { AutoFlush = true };
                await writer.WriteLineAsync("SHOW");
            }
            catch
            {
            }
        }
    }
}





