using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace BBSReader
{
    /// <summary>
    /// AdvancedKeywordDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AdvancedKeywordDialog : Window
    {
        public AdvancedKeywordDialog()
        {
            InitializeComponent();

            AuthorList = new ObservableCollection<string>();
            AuthorListBox.ItemsSource = AuthorList;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AuthorList.Add(DefaultAuthor);
            UseAuthor.IsChecked = true;

            KeywordTextBox.Text = this.Keyword;
            UseKeyword.IsChecked = true;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if ((UseKeyword.IsChecked ?? false) || (UseAuthor.IsChecked ?? false))
            {
                this.Keyword = string.IsNullOrWhiteSpace(KeywordTextBox.Text) ? "*" : KeywordTextBox.Text;
                if (AuthorList.Count == 0)
                {
                    AuthorList.Add("*");
                }
                this.DialogResult = true;
            }
        }

        public string DefaultAuthor { get; set; }
        public string Keyword { get; set; }
        public ObservableCollection<string> AuthorList;

        private void UseKeyword_Changed(object sender, RoutedEventArgs e)
        {
            if (KeywordTextBox == null)
                return;

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

        private void UseAuthor_Changed(object sender, RoutedEventArgs e)
        {
            if (AuthorList == null)
                return;

            var checkbox = sender as CheckBox;
            if (checkbox.IsChecked ?? false)
            {
                AuthorList.Clear();
                AuthorList.Add(DefaultAuthor);
                AuthorListBox.IsEnabled = true;
            }
            else
            {
                AuthorList.Clear();
                AuthorList.Add("*");
                AuthorListBox.IsEnabled = false;
            }
        }

        private void AuthorEditRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            AuthorList.RemoveAt(AuthorListBox.SelectedIndex);
            if (AuthorList.Count == 0)
            {
                UseAuthor.IsChecked = false;
            }
        }

        private void AuthorEditAddButton_Click(object sender, RoutedEventArgs e)
        {
            string line = AuthorEditBox.Text;
            if (!AuthorList.Contains(line))
            {
                if (AuthorList.Contains("*"))
                {
                    UseAuthor.IsChecked = true;
                    AuthorList.Clear();
                }
                AuthorList.Add(line);
                AuthorEditBox.Text = "";
            }
            else
            {
                AuthorEditBox.Focus();
            }
        }
    }
}
