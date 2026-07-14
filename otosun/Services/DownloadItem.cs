using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Input;
using otosun.ViewModels;

namespace otosun.Services
{
    public enum DownloadStatus
    {
        Pending,
        Downloading,
        Converting,
        Completed,
        Failed,
        Canceled
    }

    public class DownloadItem : ViewModelBase
    {
        private DownloadStatus _status = DownloadStatus.Pending;
        private double _progressValue = 0;
        private bool _isProgressIndeterminate = false;
        private string _statusText = "待機中";
        private CancellationTokenSource? _cts;

        public string Id { get; } = Guid.NewGuid().ToString();
        public string Url { get; }
        public string Title { get; }
        public string DestinationPath { get; }
        public bool IsMp3 { get; }
        
        public string FormatLabel => IsMp3 ? "MP3" : "MP4";

        public DownloadStatus Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(CanCancel));
                    OnPropertyChanged(nameof(IsActive));
                    OnPropertyChanged(nameof(IsCompleted));
                    OnPropertyChanged(nameof(IsFailedOrCanceled));
                    OnPropertyChanged(nameof(StatusBrushName));
                    
                    // Force the CommandManager to refresh command execution states
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
                    });
                }
            }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public bool IsProgressIndeterminate
        {
            get => _isProgressIndeterminate;
            set => SetProperty(ref _isProgressIndeterminate, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public bool CanCancel => Status == DownloadStatus.Pending || Status == DownloadStatus.Downloading || Status == DownloadStatus.Converting;
        public bool IsActive => Status == DownloadStatus.Downloading || Status == DownloadStatus.Converting;
        public bool IsCompleted => Status == DownloadStatus.Completed;
        public bool IsFailedOrCanceled => Status == DownloadStatus.Failed || Status == DownloadStatus.Canceled;

        // Custom status color helper
        public string StatusBrushName
        {
            get
            {
                return Status switch
                {
                    DownloadStatus.Completed => "SuccessTextBrush",
                    DownloadStatus.Failed => "SystemRedColorBrush",
                    DownloadStatus.Canceled => "TextFillColorSecondaryBrush",
                    DownloadStatus.Downloading => "SystemAccentColorBrush",
                    DownloadStatus.Converting => "SystemAccentColorBrush",
                    _ => "TextFillColorPrimaryBrush"
                };
            }
        }

        public ICommand CancelCommand { get; }

        public DownloadItem(string url, string title, string destinationPath, bool isMp3, Action<DownloadItem> onCancel)
        {
            Url = url;
            Title = title;
            DestinationPath = destinationPath;
            IsMp3 = isMp3;
            CancelCommand = new RelayCommand(() => onCancel(this), () => CanCancel);
        }

        public CancellationTokenSource CreateCancellationTokenSource()
        {
            _cts = new CancellationTokenSource();
            return _cts;
        }

        public void Cancel()
        {
            if (CanCancel)
            {
                Status = DownloadStatus.Canceled;
                StatusText = "キャンセルされました";
                ProgressValue = 0;
                IsProgressIndeterminate = false;
                
                try
                {
                    _cts?.Cancel();
                }
                catch { }
            }
        }
    }
}
