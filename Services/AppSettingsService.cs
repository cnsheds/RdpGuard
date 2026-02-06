using System;
using System.IO;
using System.Text.Json;

namespace OpenRdpGuard.Services
{
    public enum AppTheme
    {
        Light,
        Dark,
        System
    }

    public class AppSettings
    {
        public string Theme { get; set; } = AppTheme.Light.ToString();
        public bool WhitelistEnabled { get; set; } = false;
        public int BlacklistScanHours { get; set; } = 24;
        public bool BlacklistMonitoringEnabled { get; set; } = false;
        public int BlacklistMonitorIntervalMinutes { get; set; } = 10;
    }

    public interface IAppSettingsService
    {
        AppTheme GetTheme();
        void SetTheme(AppTheme theme);
        bool GetWhitelistEnabled();
        void SetWhitelistEnabled(bool enabled);
        int GetBlacklistScanHours();
        void SetBlacklistScanHours(int hours);
        bool GetBlacklistMonitoringEnabled();
        void SetBlacklistMonitoringEnabled(bool enabled);
        int GetBlacklistMonitorIntervalMinutes();
        void SetBlacklistMonitorIntervalMinutes(int minutes);
    }

    public class AppSettingsService : IAppSettingsService
    {
        private readonly string _configPath;
        private AppSettings _settings;

        public AppSettingsService()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            _settings = Load();
        }

        public AppTheme GetTheme()
        {
            return Enum.TryParse<AppTheme>(_settings.Theme, out var theme) ? theme : AppTheme.Light;
        }

        public void SetTheme(AppTheme theme)
        {
            _settings.Theme = theme.ToString();
            Save();
        }

        public bool GetWhitelistEnabled()
        {
            return _settings.WhitelistEnabled;
        }

        public void SetWhitelistEnabled(bool enabled)
        {
            _settings.WhitelistEnabled = enabled;
            Save();
        }

        public int GetBlacklistScanHours()
        {
            return _settings.BlacklistScanHours;
        }

        public void SetBlacklistScanHours(int hours)
        {
            _settings.BlacklistScanHours = hours;
            Save();
        }

        public bool GetBlacklistMonitoringEnabled()
        {
            return _settings.BlacklistMonitoringEnabled;
        }

        public void SetBlacklistMonitoringEnabled(bool enabled)
        {
            _settings.BlacklistMonitoringEnabled = enabled;
            Save();
        }

        public int GetBlacklistMonitorIntervalMinutes()
        {
            return _settings.BlacklistMonitorIntervalMinutes;
        }

        public void SetBlacklistMonitorIntervalMinutes(int minutes)
        {
            _settings.BlacklistMonitorIntervalMinutes = minutes;
            Save();
        }

        private AppSettings Load()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var data = JsonSerializer.Deserialize<AppSettings>(json);
                    if (data != null)
                    {
                        return data;
                    }
                }
            }
            catch
            {
            }

            return new AppSettings();
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch
            {
            }
        }
    }
}
