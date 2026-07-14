using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace otosun.Views
{
    public partial class DownloadPage : Page
    {
        public DownloadPage()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private async void FindSavedVideos_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            if (mainWindow == null) return;

            var dialog = new Wpf.Ui.Controls.ContentDialog(mainWindow.RootContentDialogHost)
            {
                Title = "保存した動画の確認",
                Content = "ダウンロードが完了した動画は、「履歴」ページから一覧で確認することができます。\n履歴ページから直接動画ファイルを開いたり、保存先フォルダを表示することも可能です。",
                PrimaryButtonText = "履歴ページを開く",
                CloseButtonText = "閉じる",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                mainWindow.RootNavigation.Navigate(typeof(Views.HistoryPage));
            }
        }
    }
}
