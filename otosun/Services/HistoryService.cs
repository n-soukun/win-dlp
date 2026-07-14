using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace otosun.Services
{
    /// <summary>
    /// ダウンロード履歴の1エントリを表すモデル。
    /// </summary>
    public class DownloadHistoryItem : INotifyPropertyChanged
    {
        private bool _isFileExists;
        private bool _isFileDeleted;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ThumbnailPath { get; set; } = string.Empty;
        public bool IsMp3 { get; set; }
        public DateTime DownloadedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 一度ファイルが移動または削除された場合に真となるフラグ。
        /// jsonにシリアライズされ保存される。
        /// </summary>
        public bool IsFileDeleted
        {
            get => _isFileDeleted;
            set
            {
                if (_isFileDeleted != value)
                {
                    _isFileDeleted = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ファイルが現在ディスク上に存在するかどうか。
        /// バインディング用に変更通知対応。
        /// </summary>
        [JsonIgnore]
        public bool IsFileExists
        {
            get => _isFileExists;
            set
            {
                if (_isFileExists != value)
                {
                    _isFileExists = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsFileMissing));
                }
            }
        }

        [JsonIgnore]
        public bool IsFileMissing => !IsFileExists;

        /// <summary>表示用フォーマット文字列 (MP4 or MP3)</summary>
        [JsonIgnore]
        public string FormatLabel => IsMp3 ? "MP3" : "MP4";

        /// <summary>表示用日時文字列</summary>
        [JsonIgnore]
        public string DateLabel => DownloadedAt.ToString("yyyy/MM/dd HH:mm");

        /// <summary>
        /// ディスクの現在状態を確認してIsFileExistsを更新する。
        /// 一度ファイルが検出されなくなった場合はIsFileDeletedフラグを立てて保存する。
        /// </summary>
        public void RefreshFileExists()
        {
            if (IsFileDeleted)
            {
                IsFileExists = false;
                return;
            }

            bool exists = File.Exists(FilePath);
            if (!exists)
            {
                IsFileDeleted = true;
                IsFileExists = false;
                HistoryService.Instance.Save();
            }
            else
            {
                IsFileExists = true;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// ダウンロード履歴の保存・読み込みを担うシングルトンサービス。
    /// </summary>
    public class HistoryService
    {
        private static readonly Lazy<HistoryService> _instance = new(() => new HistoryService());
        public static HistoryService Instance => _instance.Value;

        public event EventHandler? HistoryAdded;

        private const int MaxHistoryCount = 100;

        private readonly string _historyFilePath;
        private readonly object _lock = new();

        private List<DownloadHistoryItem> _allItems = new();

        private HistoryService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "otosun");
            Directory.CreateDirectory(dir);
            _historyFilePath = Path.Combine(dir, "history.json");

            Load();
        }

        /// <summary>
        /// 全履歴アイテムを新しい順で返す（呼び出し元でIsFileExistsを更新すること）。
        /// </summary>
        public IReadOnlyList<DownloadHistoryItem> GetAll()
        {
            lock (_lock)
            {
                return _allItems.OrderByDescending(x => x.DownloadedAt).ToList();
            }
        }

        /// <summary>
        /// 新しい履歴エントリを追加する。100件を超えた場合は古いものを削除する。
        /// </summary>
        public void AddEntry(string title, string filePath, string thumbnailPath, bool isMp3)
        {
            var item = new DownloadHistoryItem
            {
                Title = title,
                FilePath = filePath,
                ThumbnailPath = thumbnailPath,
                IsMp3 = isMp3,
                DownloadedAt = DateTime.Now
            };

            lock (_lock)
            {
                _allItems.Insert(0, item);

                // 100件超過分を末尾から削除（対応するサムネイルも削除）
                while (_allItems.Count > MaxHistoryCount)
                {
                    var oldest = _allItems[_allItems.Count - 1];
                    DeleteThumbnailFile(oldest.ThumbnailPath);
                    _allItems.RemoveAt(_allItems.Count - 1);
                }
            }

            Save();
            HistoryAdded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 指定IDのエントリを履歴ファイルから削除する（アイテム自体はリストに残す）。
        /// サムネイルファイルがあれば併せて削除する。
        /// </summary>
        public void RemoveEntry(string id)
        {
            string? thumbPath = null;
            lock (_lock)
            {
                var target = _allItems.Find(x => x.Id == id);
                thumbPath = target?.ThumbnailPath;
                _allItems.RemoveAll(x => x.Id == id);
            }
            DeleteThumbnailFile(thumbPath);
            Save();
        }

        public void ClearAll()
        {
            List<string> thumbPaths;
            lock (_lock)
            {
                thumbPaths = _allItems.Select(x => x.ThumbnailPath).Where(p => !string.IsNullOrEmpty(p)).ToList();
                _allItems.Clear();
            }

            foreach (var path in thumbPaths)
            {
                DeleteThumbnailFile(path);
            }

            Save();
            HistoryAdded?.Invoke(this, EventArgs.Empty);
        }

        private static void DeleteThumbnailFile(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try { File.Delete(path); } catch { }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_historyFilePath)) return;
                var json = File.ReadAllText(_historyFilePath);
                var items = JsonSerializer.Deserialize<List<DownloadHistoryItem>>(json);
                if (items != null)
                {
                    _allItems = items;
                }
            }
            catch
            {
                // 破損している場合は空の状態で起動
                _allItems = new List<DownloadHistoryItem>();
            }
        }

        public void Save()
        {
            try
            {
                string json;
                lock (_lock)
                {
                    json = JsonSerializer.Serialize(_allItems, new JsonSerializerOptions { WriteIndented = true });
                }
                File.WriteAllText(_historyFilePath, json);
            }
            catch
            {
                // 保存失敗は無視（次回以降に再試行）
            }
        }
    }
}
