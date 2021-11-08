using System.Windows;

namespace BBSReader
{
    /// <summary>
    /// ManualAddTagDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ManualAddTagDialog : Window
    {
        public string TitleText { get; set; }

        public ManualAddTagDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TitleHint.Content = TitleText;
            Tag.Text = TitleText;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
