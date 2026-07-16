using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Wpf.Ui.Controls;
using otosun.ViewModels;
using Microsoft.Toolkit.Uwp.Notifications;

namespace otosun.Services
{
    public class DownloadService : ViewModelBase
    {
        private static readonly Lazy<DownloadService> _instance = new(() => new DownloadService());
        public static DownloadService Instance => _instance.Value;

        public event Action<Type>? NavigationRequested;

        private ContentDialogHost? _contentDialogHost;

        /// <summary>
        /// MainWindow から ContentDialogHost を登録する。
        /// </summary>
        public void SetContentDialogHost(ContentDialogHost host)
        {
            _contentDialogHost = host;
        }

        private string _urlText = string.Empty;
        private bool _isMp4 = true;
        private bool _isMp3 = false;
        private bool _isDownloading = false;
        private bool _isVerifyingUrl = false;
        private string _statusText = "準備中...";
        private double _progressValue = 0;
        private bool _isProgressIndeterminate = false;
        private string _logText = string.Empty;
        private string _toolsStatusText = "確認中...";
        private bool _isToolsUpdating = false;
        private double _toolsProgressValue = 0;
        private bool _isToolsProgressIndeterminate = false;

        public string UrlText
        {
            get => _urlText;
            set => SetProperty(ref _urlText, value);
        }

        public bool IsMp4
        {
            get => _isMp4;
            set
            {
                if (SetProperty(ref _isMp4, value) && value)
                {
                    IsMp3 = false;
                }
            }
        }

        public bool IsMp3
        {
            get => _isMp3;
            set
            {
                if (SetProperty(ref _isMp3, value) && value)
                {
                    IsMp4 = false;
                }
            }
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            private set
            {
                if (SetProperty(ref _isDownloading, value))
                {
                    OnPropertyChanged(nameof(IsNotDownloading));
                    OnPropertyChanged(nameof(ActiveStatusText));
                    OnPropertyChanged(nameof(ActiveProgressValue));
                    OnPropertyChanged(nameof(ActiveIsProgressIndeterminate));
                    OnPropertyChanged(nameof(IsOperationActive));
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
                    });
                }
            }
        }

        public bool IsNotDownloading => !IsDownloading;

        public bool IsVerifyingUrl
        {
            get => _isVerifyingUrl;
            set
            {
                if (SetProperty(ref _isVerifyingUrl, value))
                {
                    OnPropertyChanged(nameof(IsNotVerifyingUrl));
                    OnPropertyChanged(nameof(CanSaveVideo));
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                        ((RelayCommand)SaveAsCommand).RaiseCanExecuteChanged();
                        ((RelayCommand)PasteCommand).RaiseCanExecuteChanged();
                    });
                }
            }
        }

        public bool IsNotVerifyingUrl => !IsVerifyingUrl;

        public string StatusText
        {
            get => _statusText;
            set
            {
                if (SetProperty(ref _statusText, value))
                {
                    OnPropertyChanged(nameof(ActiveStatusText));
                }
            }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                if (SetProperty(ref _progressValue, value))
                {
                    OnPropertyChanged(nameof(ActiveProgressValue));
                }
            }
        }

        public bool IsProgressIndeterminate
        {
            get => _isProgressIndeterminate;
            set
            {
                if (SetProperty(ref _isProgressIndeterminate, value))
                {
                    OnPropertyChanged(nameof(ActiveIsProgressIndeterminate));
                }
            }
        }

        public string LogText
        {
            get => _logText;
            set => SetProperty(ref _logText, value);
        }

        public string ToolsStatusText
        {
            get => _toolsStatusText;
            set
            {
                if (SetProperty(ref _toolsStatusText, value))
                {
                    OnPropertyChanged(nameof(ToolsStatusBrush));
                    OnPropertyChanged(nameof(ActiveStatusText));
                }
            }
        }

        public bool IsToolsUpdating
        {
            get => _isToolsUpdating;
            private set
            {
                if (SetProperty(ref _isToolsUpdating, value))
                {
                    OnPropertyChanged(nameof(ShowToolsStatus));
                    OnPropertyChanged(nameof(ActiveStatusText));
                    OnPropertyChanged(nameof(ActiveProgressValue));
                    OnPropertyChanged(nameof(ActiveIsProgressIndeterminate));
                    OnPropertyChanged(nameof(IsOperationActive));
                }
            }
        }

        public double ToolsProgressValue
        {
            get => _toolsProgressValue;
            set
            {
                if (SetProperty(ref _toolsProgressValue, value))
                {
                    OnPropertyChanged(nameof(ActiveProgressValue));
                }
            }
        }

        public bool IsToolsProgressIndeterminate
        {
            get => _isToolsProgressIndeterminate;
            set
            {
                if (SetProperty(ref _isToolsProgressIndeterminate, value))
                {
                    OnPropertyChanged(nameof(ActiveIsProgressIndeterminate));
                }
            }
        }

        public string ActiveStatusText => IsDownloading ? StatusText : (IsToolsUpdating ? ToolsStatusText : StatusText);
        public double ActiveProgressValue => IsDownloading ? ProgressValue : (IsToolsUpdating ? ToolsProgressValue : ProgressValue);
        public bool ActiveIsProgressIndeterminate => IsDownloading ? IsProgressIndeterminate : (IsToolsUpdating ? IsToolsProgressIndeterminate : IsProgressIndeterminate);
        public bool IsOperationActive => IsDownloading || IsToolsUpdating;

        private System.Windows.Media.Brush GetBrushResource(string key, System.Windows.Media.Brush fallback)
        {
            try
            {
                if (Application.Current != null && Application.Current.TryFindResource(key) is System.Windows.Media.Brush brush)
                {
                    return brush;
                }
            }
            catch { }
            return fallback;
        }

        public System.Windows.Media.Brush ToolsStatusBrush
        {
            get
            {
                if (ToolsStatusText.Contains("エラー") || ToolsStatusText.Contains("未導入"))
                {
                    return System.Windows.Media.Brushes.Red;
                }
                else if (ToolsStatusText.Contains("ダウンロード中") || ToolsStatusText.Contains("展開中") || ToolsStatusText.Contains("アップデート中") || ToolsStatusText.Contains("更新チェック") || ToolsStatusText.Contains("確認中"))
                {
                    return GetBrushResource("SystemAccentColorBrush", System.Windows.Media.Brushes.DodgerBlue);
                }
                else if (ToolsStatusText == "すべて導入済み" || ToolsStatusText == "最新状態")
                {
                    var greenBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 167, 69)); // Bootstrap success green
                    return GetBrushResource("SystemGreenColorBrush", greenBrush);
                }
                return GetBrushResource("TextFillColorPrimaryBrush", System.Windows.Media.Brushes.White);
            }
        }

        public bool IsYtDlpInstalled => File.Exists(Path.Combine(ToolService.GetToolsDir(), "yt-dlp.exe"));
        public bool IsDenoInstalled => File.Exists(Path.Combine(ToolService.GetToolsDir(), "deno.exe"));
        public bool IsFfmpegInstalled => File.Exists(Path.Combine(ToolService.GetToolsDir(), "ffmpeg.exe"));

        public bool CanSaveVideo => IsNotVerifyingUrl && IsYtDlpInstalled;

        public bool ShowToolsStatus => !IsYtDlpInstalled || !IsDenoInstalled || !IsFfmpegInstalled || IsToolsUpdating;

        public void UpdateToolsStatus(bool preserveStatusTextOnError = false, bool hasError = false)
        {
            bool ytDlp = IsYtDlpInstalled;
            bool deno = IsDenoInstalled;
            bool ffmpeg = IsFfmpegInstalled;

            OnPropertyChanged(nameof(IsYtDlpInstalled));
            OnPropertyChanged(nameof(IsDenoInstalled));
            OnPropertyChanged(nameof(IsFfmpegInstalled));
            OnPropertyChanged(nameof(CanSaveVideo));
            OnPropertyChanged(nameof(ShowToolsStatus));
            Application.Current.Dispatcher.Invoke(() =>
            {
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                ((RelayCommand)SaveAsCommand).RaiseCanExecuteChanged();
            });

            if (IsToolsUpdating)
            {
                return;
            }

            if (preserveStatusTextOnError && hasError)
            {
                return;
            }

            if (ytDlp && deno && ffmpeg)
            {
                ToolsStatusText = "すべて導入済み";
            }
            else if (!ytDlp && !deno && !ffmpeg)
            {
                ToolsStatusText = "未導入 (ダウンロード待ち)";
            }
            else
            {
                var missing = new System.Collections.Generic.List<string>();
                if (!ytDlp) missing.Add("yt-dlp");
                if (!deno) missing.Add("Deno");
                if (!ffmpeg) missing.Add("FFmpeg");
                ToolsStatusText = $"{string.Join(", ", missing)} 未導入";
            }
        }

        public async Task InitializeToolsAsync()
        {
            var toolsDir = ToolService.GetToolsDir();
            if (!Directory.Exists(toolsDir))
            {
                Directory.CreateDirectory(toolsDir);
            }
            
            UpdateToolsStatus();

            bool isYtDlpMissing = !IsYtDlpInstalled;
            bool isDenoMissing = !IsDenoInstalled;
            bool isFfmpegMissing = !IsFfmpegInstalled;

            bool needDownload = isYtDlpMissing || isDenoMissing || isFfmpegMissing;
            bool needUpdate = false;

            if (!needDownload)
            {
                var lastUpdate = SettingsService.Instance.LastUpdateTime;
                if (DateTime.Now - lastUpdate >= TimeSpan.FromHours(24))
                {
                    needUpdate = true;
                }
            }

            if (needDownload || needUpdate)
            {
                IsToolsUpdating = true;
                _ = Task.Run(async () =>
                {
                    bool hasError = false;
                    try
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            LogText = string.Empty;
                        });
                        AppendLog("=== 専用ツールの初期化・ダウンロード処理を開始しました ===");

                        if (needDownload)
                        {
                            ToolsStatusText = "一部のツールが未導入です。バックグラウンドで導入中...";
                            
                            if (isYtDlpMissing)
                            {
                                ToolsStatusText = "yt-dlp をダウンロード中...";
                                await ToolService.SetupYtDlpAsync(toolsDir, msg => AppendLog(msg), (status, prog, indet) => {
                                    if (status != null) ToolsStatusText = status;
                                    if (prog.HasValue) ToolsProgressValue = prog.Value;
                                    if (indet.HasValue) IsToolsProgressIndeterminate = indet.Value;
                                }, CancellationToken.None);
                                UpdateToolsStatus();
                            }

                            if (isDenoMissing)
                            {
                                ToolsStatusText = "Deno をダウンロード中...";
                                await ToolService.SetupDenoAsync(toolsDir, msg => AppendLog(msg), (status, prog, indet) => {
                                    if (status != null) ToolsStatusText = status;
                                    if (prog.HasValue) ToolsProgressValue = prog.Value;
                                    if (indet.HasValue) IsToolsProgressIndeterminate = indet.Value;
                                }, CancellationToken.None);
                                UpdateToolsStatus();
                            }

                            if (isFfmpegMissing)
                            {
                                ToolsStatusText = "FFmpeg をダウンロード中...";
                                await ToolService.SetupFfmpegAsync(toolsDir, msg => AppendLog(msg), (status, prog, indet) => {
                                    if (status != null) ToolsStatusText = status;
                                    if (prog.HasValue) ToolsProgressValue = prog.Value;
                                    if (indet.HasValue) IsToolsProgressIndeterminate = indet.Value;
                                }, CancellationToken.None);
                                UpdateToolsStatus();
                            }

                            SettingsService.Instance.LastUpdateTime = DateTime.Now;
                        }
                        else if (needUpdate)
                        {
                            ToolsStatusText = "起動時のアップデートを確認中...";
                            AppendLog("最後にアップデートしてから24時間以上経過したため、アップデートを確認します。");
                            
                            ToolsStatusText = "yt-dlp をアップデート中...";
                            await ToolService.SetupYtDlpAsync(toolsDir, msg => AppendLog(msg), (status, prog, indet) => {
                                if (status != null) ToolsStatusText = status;
                                if (prog.HasValue) ToolsProgressValue = prog.Value;
                                if (indet.HasValue) IsToolsProgressIndeterminate = indet.Value;
                            }, CancellationToken.None);

                            ToolsStatusText = "Deno をアップデート中...";
                            await ToolService.SetupDenoAsync(toolsDir, msg => AppendLog(msg), (status, prog, indet) => {
                                if (status != null) ToolsStatusText = status;
                                if (prog.HasValue) ToolsProgressValue = prog.Value;
                                if (indet.HasValue) IsToolsProgressIndeterminate = indet.Value;
                            }, CancellationToken.None);

                            await ToolService.SetupFfmpegAsync(toolsDir, msg => AppendLog(msg), (status, prog, indet) => {
                                if (status != null) ToolsStatusText = status;
                                if (prog.HasValue) ToolsProgressValue = prog.Value;
                                if (indet.HasValue) IsToolsProgressIndeterminate = indet.Value;
                            }, CancellationToken.None);

                            SettingsService.Instance.LastUpdateTime = DateTime.Now;
                        }

                        ToolsStatusText = "すべて導入済み";
                        AppendLog("=== 専用ツールの初期化・ダウンロード処理が完了しました ===");
                    }
                    catch (Exception ex)
                    {
                        ToolsStatusText = $"ツールの導入エラー: {ex.Message}";
                        AppendLog($"[エラー] ツールの導入処理中にエラーが発生しました: {ex.Message}");
                        hasError = true;
                    }
                    finally
                    {
                        IsToolsUpdating = false;
                        ToolsProgressValue = 0;
                        IsToolsProgressIndeterminate = false;
                        UpdateToolsStatus(preserveStatusTextOnError: true, hasError: hasError);
                    }
                });
            }
            else
            {
                UpdateToolsStatus();
            }
        }

        public ICommand PasteCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand CancelCommand { get; }

        public ObservableCollection<DownloadItem> QueueItems { get; }

        private readonly object _queueLock = new();
        private bool _isQueueRunning = false;

        private DownloadService()
        {
            PasteCommand = new RelayCommand(Paste, () => !IsVerifyingUrl);
            SaveCommand = new RelayCommand(Save, () => !IsVerifyingUrl && IsYtDlpInstalled);
            SaveAsCommand = new RelayCommand(SaveAs, () => !IsVerifyingUrl && IsYtDlpInstalled);
            CancelCommand = new RelayCommand(Cancel, () => IsDownloading);
            QueueItems = new ObservableCollection<DownloadItem>();
        }

        private void Paste()
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    UrlText = Clipboard.GetText();
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox("エラー", $"クリップボードからの貼り付けに失敗しました:\n{ex.Message}");
            }
        }

        private void Cancel()
        {
            var activeItem = QueueItems.FirstOrDefault(item => item.IsActive);
            if (activeItem != null)
            {
                activeItem.Cancel();
            }
            else
            {
                StatusText = "キャンセル中...";
            }
        }

        private async void Save()
        {
            await AddToQueueAsync(isSaveAs: false);
        }

        private async void SaveAs()
        {
            await AddToQueueAsync(isSaveAs: true);
        }

        private async Task AddToQueueAsync(bool isSaveAs)
        {
            var url = UrlText.Trim();
            if (string.IsNullOrEmpty(url))
            {
                ShowMessageBox("エラー", "ダウンロードする動画のURLを入力してください。");
                return;
            }

            IsVerifyingUrl = true;

            try
            {
                var toolsDir = ToolService.GetToolsDir();
                
                // 1. yt-dlp の導入状態を確認
                if (!IsYtDlpInstalled)
                {
                    ShowMessageBox("エラー", "yt-dlp が見つかりません。ツールの導入が完了するまでお待ちください。");
                    return;
                }

                // 2. yt-dlp を使用して動画のタイトル（メタ情報）を取得し、URLの動画が存在するか検証する
                var videoTitle = await ToolService.GetVideoTitleAsync(toolsDir, url, msg => { }, CancellationToken.None);
                var sanitizedTitle = ToolService.SanitizeFileName(videoTitle);

                string destinationPath = string.Empty;
                bool isMp3 = IsMp3;
                var filter = isMp3 ? "MP3 オーディオ|*.mp3" : "MP4 ビデオ|*.mp4";
                var defaultExt = isMp3 ? "mp3" : "mp4";
                var defaultName = isMp3 ? $"{sanitizedTitle}.mp3" : $"{sanitizedTitle}.mp4";

                // 3. 名前を付けて保存の場合は、キューに入れる時点でファイル保存ダイアログを表示
                if (isSaveAs)
                {
                    bool? dialogResult = null;
                    var saveFileDialog = new SaveFileDialog
                    {
                        Filter = filter,
                        DefaultExt = defaultExt,
                        FileName = defaultName
                    };

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        dialogResult = saveFileDialog.ShowDialog();
                        if (dialogResult == true)
                        {
                            destinationPath = saveFileDialog.FileName;
                        }
                    });

                    if (dialogResult != true)
                    {
                        // ダイアログがキャンセルされた場合はキューに追加しない
                        return;
                    }
                }
                else
                {
                    var downloadsPath = SettingsService.Instance.DownloadFolderPath;
                    if (!Directory.Exists(downloadsPath))
                    {
                        Directory.CreateDirectory(downloadsPath);
                    }

                    destinationPath = Path.Combine(downloadsPath, defaultName);
                    destinationPath = GetUniqueFilePath(destinationPath);
                }

                // 4. キューアイテムを作成して追加
                var newItem = new DownloadItem(url, videoTitle, destinationPath, isMp3, item =>
                {
                    item.Cancel();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        QueueItems.Remove(item);
                    });
                });

                Application.Current.Dispatcher.Invoke(() =>
                {
                    QueueItems.Add(newItem);
                    UrlText = string.Empty; // 入力欄をクリア
                });

                // 5. キュー処理を開始
                StartQueueIfNeeded();
            }
            catch (Exception ex)
            {
                ShowMessageBox("エラー", $"動画の検証に失敗しました。動画が存在しないか、URLが正しくありません。\n{ex.Message}");
            }
            finally
            {
                IsVerifyingUrl = false;
            }
        }

        private void StartQueueIfNeeded()
        {
            lock (_queueLock)
            {
                if (_isQueueRunning) return;
                _isQueueRunning = true;
            }

            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        // Deno, FFmpeg の導入が終わるまで待機
                        while (!IsDenoInstalled || !IsFfmpegInstalled)
                        {
                            await Task.Delay(1000);
                        }

                        DownloadItem? nextItem = null;
                        lock (_queueLock)
                        {
                            nextItem = QueueItems.FirstOrDefault(item => item.Status == DownloadStatus.Pending);
                            if (nextItem == null)
                            {
                                _isQueueRunning = false;
                                break;
                            }
                        }

                        await ProcessQueueItemAsync(nextItem);
                    }
                }
                catch (Exception)
                {
                    lock (_queueLock)
                    {
                        _isQueueRunning = false;
                    }
                }
            });
        }

        private async Task ProcessQueueItemAsync(DownloadItem item)
        {
            item.Status = DownloadStatus.Downloading;
            item.StatusText = "ダウンロード中...";
            item.ProgressValue = 0;
            item.IsProgressIndeterminate = false;

            // グローバルステータス（ProgressPage用）を更新
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsDownloading = true;
                StatusText = $"{item.Title} をダウンロード中...";
                ProgressValue = 0;
                IsProgressIndeterminate = false;
                LogText = string.Empty;
            });

            AppendLog($"=== ダウンロード処理を開始しました (形式: {(item.IsMp3 ? "MP3" : "MP4")}) ===");
            AppendLog($"対象URL: {item.Url}");
            AppendLog($"保存先: {item.DestinationPath}");

            var cts = item.CreateCancellationTokenSource();
            string? downloadedFilePath = null;
            string tempThumbPath = string.Empty;

            try
            {
                var toolsDir = ToolService.GetToolsDir();
                
                // ツールの導入状態を最終確認
                if (!IsYtDlpInstalled || !IsDenoInstalled || !IsFfmpegInstalled)
                {
                    throw new Exception("必要なツール (yt-dlp, Deno, FFmpeg) が見つかりません。ツールの導入が完了するまでお待ちください。");
                }

                var tempFileBase = Path.Combine(toolsDir, $"temp_{Guid.NewGuid()}");
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    item.StatusText = item.IsMp3 ? "音声ファイルをダウンロード中..." : "動画ファイルをダウンロード中...";
                    item.IsProgressIndeterminate = false;
                    item.ProgressValue = 0;
                    StatusText = $"{item.Title}: {item.StatusText}";
                });

                downloadedFilePath = await ToolService.DownloadVideoAsync(
                    toolsDir,
                    item.Url,
                    tempFileBase,
                    item.IsMp3,
                    msg => AppendLog(msg),
                    percent =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            item.ProgressValue = percent;
                            ProgressValue = percent;
                            var text = item.IsMp3 ? $"音声をダウンロード中... {percent:F1}%" : $"動画をダウンロード中... {percent:F1}%";
                            item.StatusText = text;
                            StatusText = $"{item.Title}: {text}";
                        });
                    },
                    cts.Token
                );

                if (string.IsNullOrEmpty(downloadedFilePath) || !File.Exists(downloadedFilePath))
                {
                    throw new Exception("ダウンロードに失敗しました。一時ファイルが見つかりません。");
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    item.Status = DownloadStatus.Converting;
                    item.StatusText = item.IsMp3 ? "音声をMP3フォーマットに変換中..." : "動画をMP4フォーマットに変換中...";
                    item.IsProgressIndeterminate = true;
                    StatusText = $"{item.Title}: {item.StatusText}";
                });

                await ToolService.ConvertFileAsync(
                    toolsDir,
                    downloadedFilePath,
                    item.DestinationPath,
                    item.IsMp3,
                    msg => AppendLog(msg),
                    (status, prog, indet) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (status != null)
                            {
                                item.StatusText = status;
                                StatusText = $"{item.Title}: {status}";
                            }
                            if (prog.HasValue)
                            {
                                item.ProgressValue = prog.Value;
                                ProgressValue = prog.Value;
                            }
                            if (indet.HasValue)
                            {
                                item.IsProgressIndeterminate = indet.Value;
                                IsProgressIndeterminate = indet.Value;
                            }
                        });
                    },
                    cts.Token
                );

                if (!item.IsMp3)
                {
                    try
                    {
                        var thumbDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "otosun", "thumbnails");
                        Directory.CreateDirectory(thumbDir);
                        var thumbFile = Path.Combine(thumbDir, $"{Guid.NewGuid():N}.jpg");
                        await ToolService.ExtractThumbnailAsync(toolsDir, item.DestinationPath, thumbFile, msg => AppendLog(msg), cts.Token);
                        tempThumbPath = thumbFile;
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"[警告] サムネイルの抽出に失敗しました: {ex.Message}");
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    item.Status = DownloadStatus.Completed;
                    item.StatusText = "完了";
                    item.ProgressValue = 100;
                    item.IsProgressIndeterminate = false;
                });

                // 履歴に記録
                HistoryService.Instance.AddEntry(item.Title, item.DestinationPath, tempThumbPath, item.IsMp3);

                // Windowsトースト通知を表示
                try
                {
                    var builder = new ToastContentBuilder()
                        .AddText("ダウンロード完了")
                        .AddText($"「{item.Title}」のダウンロードが完了しました。");

                    if (!string.IsNullOrEmpty(tempThumbPath) && File.Exists(tempThumbPath))
                    {
                        builder.AddInlineImage(new Uri(Path.GetFullPath(tempThumbPath)));
                    }

                    builder.Show();
                }
                catch (Exception ex)
                {
                    AppendLog($"[警告] 通知の送信に失敗しました: {ex.Message}");
                }
            }
            catch (OperationCanceledException)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    item.Status = DownloadStatus.Canceled;
                    item.StatusText = "キャンセルされました";
                    item.ProgressValue = 0;
                    item.IsProgressIndeterminate = false;
                });
                AppendLog("ダウンロードがキャンセルされました。");
                ShowMessageBox("キャンセル", $"{item.Title} のダウンロードがキャンセルされました。");
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    item.Status = DownloadStatus.Failed;
                    item.StatusText = "エラー";
                    item.ProgressValue = 0;
                    item.IsProgressIndeterminate = false;
                });
                AppendLog($"エラーが発生しました: {ex.Message}");
                ShowMessageBox("エラー", $"{item.Title} のダウンロード中にエラーが発生しました:\n{ex.Message}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(downloadedFilePath) && File.Exists(downloadedFilePath))
                {
                    try
                    {
                        File.Delete(downloadedFilePath);
                    }
                    catch { }
                }

                cts.Dispose();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    QueueItems.Remove(item);

                    // 未完了のアイテムがあるか確認
                    bool anyActiveOrPending = QueueItems.Any(qi => qi.Status == DownloadStatus.Pending || qi.Status == DownloadStatus.Downloading || qi.Status == DownloadStatus.Converting);

                    if (!anyActiveOrPending)
                    {
                        IsDownloading = false;
                        StatusText = "ダウンロード完了";
                        ProgressValue = 100;
                        IsProgressIndeterminate = false;
                    }
                });
            }
        }

        private const int MaxLogLines = 200; // 最大200行に制限

        private void AppendLog(string line)
        {
            var sb = new StringBuilder(LogText);
            sb.AppendLine(line);

            var currentLog = sb.ToString();
            var lines = currentLog.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            if (lines.Length > MaxLogLines)
            {
                int skipCount = lines.Length - MaxLogLines;
                var newLogBuilder = new StringBuilder();
                for (int i = skipCount; i < lines.Length; i++)
                {
                    if (i == lines.Length - 1 && string.IsNullOrEmpty(lines[i]))
                    {
                        continue;
                    }
                    newLogBuilder.AppendLine(lines[i]);
                }
                LogText = newLogBuilder.ToString();
            }
            else
            {
                LogText = currentLog;
            }
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


        private string GetUniqueFilePath(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return filePath;
            }

            var directory = Path.GetDirectoryName(filePath) ?? "";
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);

            int count = 1;
            string newFilePath;
            do
            {
                newFilePath = Path.Combine(directory, $"{fileNameWithoutExt} ({count}){extension}");
                count++;
            } while (File.Exists(newFilePath));

            return newFilePath;
        }
    }
}
