using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using otosun.ViewModels;

namespace otosun.Views
{
    public partial class HistoryPage : Page
    {
        private ScrollViewer? _parentScrollViewer;

        public HistoryPage()
        {
            InitializeComponent();

            this.Loaded += HistoryPage_Loaded;
            this.Unloaded += HistoryPage_Unloaded;
        }

        private void HistoryPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is HistoryPageViewModel vm)
            {
                vm.Refresh();
            }

            // 親の ScrollViewer（NavigationViewに内包されているもの）を探索してイベントを購読
            if (_parentScrollViewer == null)
            {
                _parentScrollViewer = FindVisualAncestor<ScrollViewer>(this);
                if (_parentScrollViewer != null)
                {
                    _parentScrollViewer.ScrollChanged += ParentScrollViewer_ScrollChanged;
                }
            }
        }

        private void HistoryPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_parentScrollViewer != null)
            {
                _parentScrollViewer.ScrollChanged -= ParentScrollViewer_ScrollChanged;
                _parentScrollViewer = null;
            }
        }

        /// <summary>
        /// ビジュアルツリーを遡って、指定された型の親要素を検索するヘルパーメソッド。
        /// </summary>
        private static T? FindVisualAncestor<T>(DependencyObject? obj) where T : DependencyObject
        {
            while (obj != null)
            {
                if (obj is T ancestor)
                {
                    return ancestor;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        public void ScrollToTop()
        {
            _parentScrollViewer?.ScrollToTop();
        }

        /// <summary>
        /// サムネイル画像を Tag に指定されたパスからメモリロードする。
        /// </summary>
        private void ThumbnailImage_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Image img) return;
            var path = img.Tag as string;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                img.Source = bitmap;
            }
            catch
            {
                // 読み込み失敗時はフォールバックアイコンのまま
            }
        }

        /// <summary>
        /// スクロールが末尾付近に達したら追加ロードを実行する（無限スクロール）。
        /// 親の ScrollViewer からイベントを受け取る。
        /// </summary>
        private void ParentScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is not ScrollViewer sv) return;
            if (DataContext is not HistoryPageViewModel vm) return;

            // ViewportHeightが0の場合は配置完了前なので無視
            if (sv.ViewportHeight <= 0) return;

            // 残りスクロール量が200px以下になったら追加ロード
            var distanceToBottom = sv.ExtentHeight - sv.VerticalOffset - sv.ViewportHeight;
            if (distanceToBottom < 200 && vm.HasMoreItems)
            {
                vm.LoadMoreCommand.Execute(null);
            }
        }
    }
}
