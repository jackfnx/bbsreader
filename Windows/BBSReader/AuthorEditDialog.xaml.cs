using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace BBSReader
{
    /// <summary>
    /// AuthorEditDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AuthorEditDialog : Window
    {
        public ObservableCollection<string> Authors { get; set; }
        public ObservableCollection<object> Keywords { get; set; }
        public ObservableCollection<object> Articles { get; set; }

        public AuthorEditDialog()
        {
            InitializeComponent();

            Authors = new ObservableCollection<string>();
            AuthorsBox.ItemsSource = Authors;

            Keywords = new ObservableCollection<object>();
            KeywordsBox.DataContext = Keywords;

            Articles = new ObservableCollection<object>();
            ArticlesBox.DataContext = Articles;
        }

        public void Initialize(List<string> authors, List<List<string>> subKeywords, List<int> subReads, List<string> titles)
        {
            Authors.Clear();
            Keywords.Clear();
            Articles.Clear();
            authors.ForEach(x => Authors.Add(x));
            for (int i = 0; i < subKeywords.Count; i++)
            {
                var subKw = subKeywords[i];
                var subRead = subReads[i];
                var tooltip = MakeTooltip(subKw, subRead);
                Keywords.Add(new { Title = subKw[0], SubKeywords = subKw, SubRead = subRead, Tooltip = tooltip });
            }
            titles.ForEach(x => Articles.Add(new { Title = x, Match = subKeywords.FindIndex(y => !y.TrueForAll(z=> !x.Contains(z))) != -1 }));
        }

        private string MakeTooltip(List<string> subKeywords, int subRead)
        {
            return string.Format("{0}\nUnread:\t{1}", string.Join("\n", subKeywords.ConvertAll(x => string.Format("* {0}", x))), subRead);
        }

        private void RemoveNameButton_Click(object sender, RoutedEventArgs e)
        {
            if ((AuthorsBox.SelectedIndex == -1) || (AuthorsBox.SelectedIndex == 0))
            {
                return;
            }
            Authors.RemoveAt(AuthorsBox.SelectedIndex);
        }

        private void AddNameButton_Click(object sender, RoutedEventArgs e)
        {
            string line = AuthorEditBox.Text;
            if (!Authors.Contains(line))
            {
                Authors.Add(line);
                AuthorEditBox.Text = "";
            }
            else
            {
                AuthorEditBox.Focus();
            }
        }

        private void RemoveKeywordButton_Click(object sender, RoutedEventArgs e)
        {
            if (KeywordsBox.SelectedIndex == -1)
            {
                return;
            }
            dynamic kw = Keywords[KeywordsBox.SelectedIndex];
            Keywords.RemoveAt(KeywordsBox.SelectedIndex);
            UpdatePreview(null, kw.Title);
        }

        private void AddKeywordButton_Click(object sender, RoutedEventArgs e)
        {
            string line = KeywordEditBox.Text;
            if (!Keywords.Contains(line))
            {
                List<string> subKeywords = new List<string>() { line };
                Keywords.Add(new
                {
                    Title = line,
                    SubKeywords = subKeywords,
                    SubRead = -1,
                    Tooltip = MakeTooltip(subKeywords, -1)
                });
                KeywordEditBox.Text = "";
                UpdatePreview(line, null);
            }
            else
            {
                KeywordEditBox.Focus();
            }
        }

        private void UpdatePreview(dynamic add, dynamic remove)
        {
            for (int i = 0; i < Articles.Count; i++)
            {
                dynamic ali = Articles[i];
                if (add != null)
                {
                    if (ali.Title.Contains(add))
                    {
                        Articles[i] = new { Title = ali.Title, Match = true };
                    }
                }
                if (remove != null)
                {
                    if (ali.Title.Contains(remove))
                    {
                        Articles[i] = new { Title = ali.Title, Match = false };
                    }
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void ListViewItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListViewItem lvi = sender as ListViewItem;
            dynamic item = lvi.Content;
            List<string> oldSubKeywords = new List<string>(item.SubKeywords);
            int subRead = item.SubRead;
            AliasEditDialog dialog = new AliasEditDialog()
            {
                Owner = this,
                Keyword = "*"
            };
            oldSubKeywords.ForEach(x => dialog.Aliases.Add(x));
            if (dialog.ShowDialog() ?? false)
            {
                int i = Keywords.IndexOf(item);
                List<string> newSubKeywords = new List<string>(dialog.Aliases);
                oldSubKeywords.ForEach(x => UpdatePreview(null, x));
                newSubKeywords.ForEach(x => UpdatePreview(x, null));
                Keywords[i] = new
                {
                    Title = dialog.Aliases[0],
                    SubKeywords = newSubKeywords,
                    SubRead = subRead,
                    Tooltip = MakeTooltip(item.SubKeywords, subRead)
                };
            }
        }
    }
}
