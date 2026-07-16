using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using otosun.Services;
using Wpf.Ui.Controls;

namespace otosun.ViewModels
{
    public class SettingsPageViewModel : ViewModelBase
    {
        private string _appVersion = "0.0.1";
        private string _ytDlpVersion = "確認中...";
        private string _denoVersion = "確認中...";
        private string _ffmpegVersion = "確認中...";
        private bool _isBusy = false;
        private string _progressText = "更新しています...";
        private CancellationTokenSource? _settingsCts;

        public string AppVersion
        {
            get => _appVersion;
            set => SetProperty(ref _appVersion, value);
        }

        public string YtDlpVersion
        {
            get => _ytDlpVersion;
            set => SetProperty(ref _ytDlpVersion, value);
        }

        public string DenoVersion
        {
            get => _denoVersion;
            set => SetProperty(ref _denoVersion, value);
        }

        public string FfmpegVersion
        {
            get => _ffmpegVersion;
            set => SetProperty(ref _ffmpegVersion, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    ((RelayCommand)UpdateYtDlpCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)UpdateDenoCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)UpdateFfmpegCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        private string _downloadFolderPath = string.Empty;

        public string DownloadFolderPath
        {
            get => _downloadFolderPath;
            set => SetProperty(ref _downloadFolderPath, value);
        }

        public ICommand UpdateYtDlpCommand { get; }
        public ICommand UpdateDenoCommand { get; }
        public ICommand UpdateFfmpegCommand { get; }
        public ICommand OpenLicenseCommand { get; }
        public ICommand OpenGitHubCommand { get; }
        public ICommand ChangeFolderCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand ClearHistoryCommand { get; }

        public SettingsPageViewModel()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            AppVersion = version != null ? $"バージョン {version.Major}.{version.Minor}.{version.Build}" : "バージョン 不明";

            DownloadFolderPath = SettingsService.Instance.DownloadFolderPath;

            UpdateYtDlpCommand = new RelayCommand(UpdateYtDlp, () => !IsBusy && !DownloadService.Instance.IsToolsUpdating);
            UpdateDenoCommand = new RelayCommand(UpdateDeno, () => !IsBusy && !DownloadService.Instance.IsToolsUpdating);
            UpdateFfmpegCommand = new RelayCommand(UpdateFfmpeg, () => !IsBusy && !DownloadService.Instance.IsToolsUpdating);
            OpenLicenseCommand = new RelayCommand(OpenLicense);
            OpenGitHubCommand = new RelayCommand(OpenGitHub);
            ChangeFolderCommand = new RelayCommand(ChangeFolder);
            OpenFolderCommand = new RelayCommand(OpenFolder);
            ClearHistoryCommand = new RelayCommand(ClearHistory);

            DownloadService.Instance.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(DownloadService.IsToolsUpdating))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ((RelayCommand)UpdateYtDlpCommand).RaiseCanExecuteChanged();
                        ((RelayCommand)UpdateDenoCommand).RaiseCanExecuteChanged();
                        ((RelayCommand)UpdateFfmpegCommand).RaiseCanExecuteChanged();
                    });

                    if (!DownloadService.Instance.IsToolsUpdating)
                    {
                        await RefreshVersionsAsync();
                    }
                }
            };
        }

        private void ChangeFolder()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "保存先フォルダの選択",
                InitialDirectory = DownloadFolderPath
            };

            if (dialog.ShowDialog() == true)
            {
                SettingsService.Instance.DownloadFolderPath = dialog.FolderName;
                DownloadFolderPath = dialog.FolderName;
            }
        }

        private void OpenFolder()
        {
            if (System.IO.Directory.Exists(DownloadFolderPath))
            {
                try
                {
                    Process.Start("explorer.exe", $"\"{DownloadFolderPath}\"");
                }
                catch { }
            }
        }

        private static void OpenLicense()
        {
            Process.Start(new ProcessStartInfo("https://opensource.org/licenses/MIT") { UseShellExecute = true });
        }

        private static void OpenGitHub()
        {
            Process.Start(new ProcessStartInfo("https://github.com/") { UseShellExecute = true });
        }

        public async Task RefreshVersionsAsync()
        {
            var toolsDir = ToolService.GetToolsDir();
            
            YtDlpVersion = "確認中...";
            DenoVersion = "確認中...";
            FfmpegVersion = "確認中...";

            var ytDlpTask = ToolService.GetYtDlpVersionAsync(toolsDir);
            var denoTask = ToolService.GetDenoVersionAsync(toolsDir);
            var ffmpegTask = ToolService.GetFfmpegVersionAsync(toolsDir);

            await Task.WhenAll(ytDlpTask, denoTask, ffmpegTask);

            YtDlpVersion = $"現在のバージョン: {await ytDlpTask}";
            DenoVersion = $"現在のバージョン: {await denoTask}";
            FfmpegVersion = $"現在のバージョン: {await ffmpegTask}";
        }

        private async void UpdateYtDlp()
        {
            await RunUpdateTaskAsync("yt-dlp", async (toolsDir, log, progress, token) =>
            {
                await ToolService.SetupYtDlpAsync(toolsDir, log, progress, token);
            });
        }

        private async void UpdateDeno()
        {
            await RunUpdateTaskAsync("Deno", async (toolsDir, log, progress, token) =>
            {
                await ToolService.SetupDenoAsync(toolsDir, log, progress, token);
            });
        }

        private async void UpdateFfmpeg()
        {
            await RunUpdateTaskAsync("FFmpeg", async (toolsDir, log, progress, token) =>
            {
                await ToolService.SetupFfmpegAsync(toolsDir, log, progress, token);
            });
        }

        private async Task RunUpdateTaskAsync(string toolName, Func<string, Action<string>, Action<string?, double?, bool?>, CancellationToken, Task> updateAction)
        {
            IsBusy = true;
            ProgressText = $"{toolName} を更新しています...";
            _settingsCts = new CancellationTokenSource();

            try
            {
                var toolsDir = ToolService.GetToolsDir();
                await updateAction(
                    toolsDir,
                    msg => { /* ログ */ },
                    (status, prog, indet) => { if (status != null) ProgressText = status; },
                    _settingsCts.Token
                );
                await RefreshVersionsAsync();
                SettingsService.Instance.LastUpdateTime = DateTime.Now;
                DownloadService.Instance.UpdateToolsStatus();
                
                ShowMessageBox("成功", $"{toolName} のインストール・更新が完了しました。");
            }
            catch (OperationCanceledException)
            {
                ShowMessageBox("キャンセル", "更新処理がキャンセルされました。");
            }
            catch (Exception ex)
            {
                ShowMessageBox("エラー", $"更新中にエラーが発生しました:\n{ex.Message}");
            }
            finally
            {
                _settingsCts?.Dispose();
                _settingsCts = null;
                IsBusy = false;
            }
        }

        public void CancelUpdate()
        {
            _settingsCts?.Cancel();
        }

        private void ShowMessageBox(string title, string content)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                var activeWindow = Application.Current.MainWindow;
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Owner = activeWindow,
                    Title = title,
                    Content = content,
                    CloseButtonText = "OK"
                };
                await messageBox.ShowDialogAsync();
            });
        }

        private void ClearHistory()
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                var activeWindow = Application.Current.MainWindow as MainWindow;
                if (activeWindow == null) return;

                var dialog = new Wpf.Ui.Controls.ContentDialog(activeWindow.RootContentDialogHost)
                {
                    Title = "履歴のクリア",
                    Content = "すべてのダウンロード履歴を消去しますか？\n（実際の動画ファイルは削除されません）",
                    PrimaryButtonText = "クリア",
                    CloseButtonText = "キャンセル",
                    DefaultButton = ContentDialogButton.Close
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    HistoryService.Instance.ClearAll();
                    ShowMessageBox("成功", "すべての履歴をクリアしました。");
                }
            });
        }
    }
}
