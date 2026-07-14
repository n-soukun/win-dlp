using otosun.Services;

namespace otosun.ViewModels
{
    public class DownloadPageViewModel : ViewModelBase
    {
        public DownloadService Download => DownloadService.Instance;
    }
}
