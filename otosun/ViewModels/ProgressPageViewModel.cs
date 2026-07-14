using otosun.Services;

namespace otosun.ViewModels
{
    public class ProgressPageViewModel : ViewModelBase
    {
        public DownloadService Download => DownloadService.Instance;
    }
}
