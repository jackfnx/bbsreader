using System.Windows;

namespace BBSReader
{
    /// <summary>
    /// ManualDownloadDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ManualDownloadDialog : Window
    {
        public ManualDownloadDialog()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
