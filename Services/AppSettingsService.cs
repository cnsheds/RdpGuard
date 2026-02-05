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
    }

    public interface IAppSettingsService
    {
        AppTheme GetTheme();
        void SetTheme(AppTheme theme);
        bool GetWhitelistEnabled();
        void SetWhitelistEnabled(bool enabled);
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
