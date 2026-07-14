using System.Windows;
using System.Windows.Controls;
using otosun.ViewModels;

namespace otosun.Views
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsPageViewModel viewModel)
            {
                await viewModel.RefreshVersionsAsync();
            }
        }
    }
}
