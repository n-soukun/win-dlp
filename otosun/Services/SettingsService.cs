using System;
using System.IO;
using System.Text.Json;

namespace otosun.Services
{
    public class AppSettings
    {
        public string DownloadFolderPath { get; set; } = string.Empty;
        public DateTime LastUpdateTime { get; set; } = DateTime.MinValue;
    }

    public class SettingsService
    {
        private static readonly Lazy<SettingsService> _instance = new(() => new SettingsService());
        public static SettingsService Instance => _instance.Value;

        private readonly string _settingsFilePath;
        private readonly object _lock = new();
        private AppSettings _settings = new();

        private SettingsService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "otosun");
            Directory.CreateDirectory(dir);
            _settingsFilePath = Path.Combine(dir, "settings.json");
            Load();
        }

        public string DownloadFolderPath
        {
            get
            {
                lock (_lock)
                {
                    if (string.IsNullOrEmpty(_settings.DownloadFolderPath))
                    {
                        // デフォルトでユーザーの「ダウンロード」フォルダーを返す
                        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    }
                    return _settings.DownloadFolderPath;
                }
            }
            set
            {
                lock (_lock)
                {
                    _settings.DownloadFolderPath = value;
                }
                Save();
            }
        }

        public DateTime LastUpdateTime
        {
            get
            {
                lock (_lock)
                {
                    return _settings.LastUpdateTime;
                }
            }
            set
            {
                lock (_lock)
                {
                    _settings.LastUpdateTime = value;
                }
                Save();
            }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        _settings = settings;
                    }
                }
            }
            catch
            {
                _settings = new AppSettings();
            }
        }

        private void Save()
        {
            try
            {
                string json;
                lock (_lock)
                {
                    json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                }
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // 保存失敗は無視
            }
        }
    }
}
