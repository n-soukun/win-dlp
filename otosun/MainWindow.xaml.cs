using System;
using System.Text;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using otosun.ViewModels;
using otosun.Services;
using Microsoft.Toolkit.Uwp.Notifications;

namespace otosun
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            SystemThemeWatcher.Watch(this);
            ApplicationAccentColorManager.ApplySystemAccent();
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            DownloadService.Instance.NavigationRequested += DownloadService_NavigationRequested;
            HistoryService.Instance.HistoryAdded += HistoryService_HistoryAdded;
            SizeChanged += MainWindow_SizeChanged;

            RootNavigation.PaneOpened += (sender, args) =>
            {
                if (!_isAutomaticChange)
                {
                    _userManuallyClosed = false;
                }
            };

            RootNavigation.PaneClosed += (sender, args) =>
            {
                if (!_isAutomaticChange)
                {
                    _userManuallyClosed = true;
                }
            };

            // トースト通知クリック時のハンドラを登録
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                Dispatcher.Invoke(() =>
                {
                    // アプリケーションを前面に表示
                    if (WindowState == WindowState.Minimized)
                    {
                        WindowState = WindowState.Normal;
                    }
                    Activate();

                    // 履歴ページへ遷移、または既に開かれていれば一番上までスクロール
                    if (_currentPage is Views.HistoryPage historyPage)
                    {
                        historyPage.ScrollToTop();
                    }
                    else
                    {
                        RootNavigation.Navigate(typeof(Views.HistoryPage));
                    }
                });
            };
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // DownloadService にダイアログホストを登録
            DownloadService.Instance.SetContentDialogHost(RootContentDialogHost);

            // 初回起動時にダウンロード画面へ遷移
            RootNavigation.Navigate(typeof(Views.DownloadPage));

            // バックグラウンドでツールの初期化/ダウンロード/アップデートを実行
            await DownloadService.Instance.InitializeToolsAsync();
        }

        private SettingsPageViewModel? _settingsViewModel;
        private object? _currentPage;
        private bool _wasNarrow;
        private bool _userManuallyClosed;
        private bool _isAutomaticChange;

        private void RootNavigation_Navigated(object sender, Wpf.Ui.Controls.NavigatedEventArgs e)
        {
            _currentPage = e.Page;

            if (e.Page is Views.HistoryPage)
            {
                HistoryNavItem.InfoBadge = null;
            }
            // 以前の変更監視を解除
            if (_settingsViewModel != null)
            {
                _settingsViewModel.PropertyChanged -= SettingsViewModel_PropertyChanged;
                _settingsViewModel = null;
            }

            // 新しいページの ViewModel の監視を開始
            if (e.Page is Views.SettingsPage settingsPage)
            {
                _settingsViewModel = settingsPage.DataContext as SettingsPageViewModel;
                if (_settingsViewModel != null)
                {
                    _settingsViewModel.PropertyChanged += SettingsViewModel_PropertyChanged;
                    // 現在の IsBusy の状態を反映
                    RootNavigation.IsEnabled = !_settingsViewModel.IsBusy;
                }
            }
            else
            {
                // 設定画面以外ではナビゲーションは常に有効
                RootNavigation.IsEnabled = true;
            }
        }

        private void SettingsViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsPageViewModel.IsBusy) && _settingsViewModel != null)
            {
                RootNavigation.IsEnabled = !_settingsViewModel.IsBusy;
            }
        }

        private void DownloadService_NavigationRequested(Type pageType)
        {
            Dispatcher.Invoke(() =>
            {
                RootNavigation.Navigate(pageType);
            });
        }

        private void HistoryService_HistoryAdded(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_currentPage is not Views.HistoryPage)
                {
                    HistoryNavItem.InfoBadge = new InfoBadge { Severity = InfoBadgeSeverity.Attention, Margin = new Thickness(0, 0, 10, 0) };
                    HistoryNavItem.InfoBadge.SetResourceReference(InfoBadge.StyleProperty, "DotInfoBadgeStyle");
                }
            });
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 幅が1200px未満を「狭い」とし、状態が変わったときだけサイドバーを開閉する
            bool isNarrow = e.NewSize.Width < 1200;
            if (isNarrow != _wasNarrow)
            {
                _wasNarrow = isNarrow;
                if (isNarrow)
                {
                    _isAutomaticChange = true;
                    RootNavigation.IsPaneOpen = false;
                    _isAutomaticChange = false;
                }
                else
                {
                    // 手動で閉じられていない場合のみ、自動で開く
                    if (!_userManuallyClosed)
                    {
                        _isAutomaticChange = true;
                        RootNavigation.IsPaneOpen = true;
                        _isAutomaticChange = false;
                    }
                }
            }
        }
    }
}