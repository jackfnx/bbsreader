using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BBSReader
{
    /// <summary>
    /// FollowDialog.xaml 的交互逻辑
    /// </summary>
    public partial class FollowDialog : Window
    {
        public FollowDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AuthorTextBox.Text = this.Author;
            KeywordTextBox.Text = this.Keyword;
            UseKeyword.IsChecked = true;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Keyword = string.IsNullOrWhiteSpace(KeywordTextBox.Text) ? "*" : KeywordTextBox.Text;
            this.DialogResult = true;
        }

        public string Keyword { get; set; }
        public string Author { get; set; }

        private void UseKeyword_Changed(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox.IsChecked ?? false)
            {
                KeywordTextBox.Text = Keyword;
                KeywordTextBox.IsEnabled = true;
            }
            else
            {
                KeywordTextBox.Text = "*";
                KeywordTextBox.IsEnabled = false;
            }

        }
    }
}
