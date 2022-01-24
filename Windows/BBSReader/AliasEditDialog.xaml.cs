using System.Collections.ObjectModel;
using System.Windows;

namespace BBSReader
{
    /// <summary>
    /// ManualAddTagDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AliasEditDialog : Window
    {
        public string Keyword { get; set; }

        public ObservableCollection<string> Aliases { get; set; }

        public AliasEditDialog()
        {
            InitializeComponent();

            Aliases = new ObservableCollection<string>();
            AliasBox.ItemsSource = Aliases;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TagWord.Content = Keyword;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Aliases.RemoveAt(AliasBox.SelectedIndex);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string line = AliasEditBox.Text;
            if (!Aliases.Contains(line))
            {
                Aliases.Add(line);
                AliasEditBox.Text = "";
            }
            else
            {
                AliasEditBox.Focus();
            }
        }
    }
}
