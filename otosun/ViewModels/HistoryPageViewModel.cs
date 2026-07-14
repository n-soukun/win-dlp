using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using otosun.Services;

namespace otosun.ViewModels
{
    public class HistoryPageViewModel : ViewModelBase
    {
        private const int PageSize = 20;

        private readonly System.Collections.Generic.List<DownloadHistoryItem> _allItems = new();
        private int _loadedCount = 0;
        private bool _hasMoreItems;

        public ObservableCollection<DownloadHistoryItem> VisibleItems { get; } = new();

        public bool HasMoreItems
        {
            get => _hasMoreItems;
            private set => SetProperty(ref _hasMoreItems, value);
        }

        public ICommand LoadMoreCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand DeleteFileCommand { get; }

        public HistoryPageViewModel()
        {
            LoadMoreCommand = new RelayCommand(LoadMore, () => HasMoreItems);
            OpenFileCommand = new RelayCommand<DownloadHistoryItem>(OpenFile);
            OpenFolderCommand = new RelayCommand<DownloadHistoryItem>(OpenFolder);
            DeleteFileCommand = new RelayCommand<DownloadHistoryItem>(DeleteFile);
        }

        /// <summary>
        /// ページが表示されるたびに履歴を再ロードし、ファイル存在状態を更新する。
        /// </summary>
        public void Refresh()
        {
            _allItems.Clear();
            _allItems.AddRange(HistoryService.Instance.GetAll());

            // ファイル存在チェック
            foreach (var item in _allItems)
            {
                item.RefreshFileExists();
            }

            VisibleItems.Clear();
            _loadedCount = 0;
            LoadMore();
        }

        private void LoadMore()
        {
            var toAdd = _allItems.Skip(_loadedCount).Take(PageSize).ToList();
            foreach (var item in toAdd)
            {
                VisibleItems.Add(item);
            }
            _loadedCount += toAdd.Count;
            HasMoreItems = _loadedCount < _allItems.Count;
            ((RelayCommand)LoadMoreCommand).RaiseCanExecuteChanged();
        }

        private void OpenFile(DownloadHistoryItem? item)
        {
            if (item == null || !item.IsFileExists) return;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.FilePath) { UseShellExecute = true });
            }
            catch { }
        }

        private void OpenFolder(DownloadHistoryItem? item)
        {
            if (item == null || !item.IsFileExists) return;
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{item.FilePath}\"");
            }
            catch { }
        }

        private void DeleteFile(DownloadHistoryItem? item)
        {
            if (item == null || !item.IsFileExists) return;
            try
            {
                File.Delete(item.FilePath);
            }
            catch { }

            // ファイル状態を更新（リストには残す）
            item.IsFileDeleted = true;
            item.IsFileExists = false;
            HistoryService.Instance.Save();
        }
    }
}
