using System.Windows.Controls;

namespace otosun.Views
{
    public partial class ProgressPage : Page
    {
        public ProgressPage()
        {
            InitializeComponent();
        }

        private void LogTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.ScrollToEnd();
                UpdateLogOpacityMask();
            }
        }

        private void Page_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            UpdateLogMaxHeight();
        }

        private void UpdateLogMaxHeight()
        {
            if (LogTextBox == null) return;

            // Pageの現在のActualHeightを取得
            double pageHeight = this.ActualHeight;
            if (pageHeight <= 0) return;

            // 各固定要素の高さを差し引く
            // マージン：上30、下40 = 70
            // キャンセルボタンエリア（行3）：高さ40 + マージン20 = 60
            // プログレスバーエリア（行1）：高さ8 + マージン上5下15 = 28
            // ステータスエリア（行0）：約40
            // 余裕を持ってさらに少し引く（安全マージン）
            double occupiedHeight = 30 + 40 + 60 + 28 + 40 + 20; // 合計約218

            double maxLogHeight = pageHeight - occupiedHeight;

            if (maxLogHeight < 50) maxLogHeight = 50; // 最低限の高さ確保

            LogTextBox.MaxHeight = maxLogHeight;
            LogTextBox.Height = maxLogHeight; // 高さをこれに固定する

            UpdateLogOpacityMask();
        }

        private void UpdateLogOpacityMask()
        {
            if (LogTextBox == null) return;

            // テキスト全体の高さが、表示領域の物理的な高さを超えているかチェック
            if (LogTextBox.ExtentHeight > LogTextBox.ViewportHeight)
            {
                if (LogTextBox.OpacityMask == null)
                {
                    var brush = new System.Windows.Media.LinearGradientBrush
                    {
                        StartPoint = new System.Windows.Point(0, 0),
                        EndPoint = new System.Windows.Point(0, 1)
                    };
                    brush.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Colors.Transparent, 0.0));
                    brush.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Colors.Transparent, 0.05));
                    brush.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Colors.Black, 0.3));
                    brush.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Colors.Black, 1.0));

                    LogTextBox.OpacityMask = brush;
                }
            }
            else
            {
                // 超過していない場合は透過グラデーションをかけない
                LogTextBox.OpacityMask = null;
            }
        }
    }
}
