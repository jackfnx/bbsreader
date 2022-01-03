using BBSReader.Data;
using System.Collections.Generic;
using System.Windows;

namespace BBSReader
{
    /// <summary>
    /// ManualDownloadDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ManualDownloadDialog : Window
    {
        public string ThreadId { get; private set; }
        public int BBSId { get; private set; }
        public bool AddToSinglesTopic { get; private set; }

        public ManualDownloadDialog()
        {
            InitializeComponent();
            List<string> siteNames = new List<BBSDef>(Constants.SITE_DEF.Values).FindAll(x => x.onlineUpdate).ConvertAll(x => x.siteName);
            BBSSelector.ItemsSource = siteNames;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            BBSId = BBSSelector.SelectedIndex;
            ThreadId = ThreadIdText.Text;
            AddToSinglesTopic = AddTo.IsChecked ?? false;
            if (BBSId >= 0 && ThreadId.Length > 0)
            {
                this.DialogResult = true;
            }
        }
    }
}
