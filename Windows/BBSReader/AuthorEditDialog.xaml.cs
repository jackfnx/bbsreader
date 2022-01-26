using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace BBSReader
{
    /// <summary>
    /// AuthorEditDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AuthorEditDialog : Window
    {
        public ObservableCollection<string> Authors { get; set; }
        public ObservableCollection<string> Keywords { get; set; }
        public ObservableCollection<object> Articles { get; set; }

        public AuthorEditDialog()
        {
            InitializeComponent();

            Authors = new ObservableCollection<string>();
            AuthorsBox.ItemsSource = Authors;

            Keywords = new ObservableCollection<string>();
            KeywordsBox.ItemsSource = Keywords;

            Articles = new ObservableCollection<object>();
            ArticlesBox.DataContext = Articles;
        }

        public void Initialize(List<string> authors, List<string> alias, List<string> titles)
        {
            authors.ForEach(x => Authors.Add(x));
            alias.ForEach(x => Keywords.Add(x));
            titles.ForEach(x => Articles.Add(new { Title = x, Match = alias.FindIndex(y => x.Contains(y)) != -1 }));
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
            string kw = Keywords[KeywordsBox.SelectedIndex];
            Keywords.RemoveAt(KeywordsBox.SelectedIndex);
            UpdatePreview(null, kw);
        }

        private void AddKeywordButton_Click(object sender, RoutedEventArgs e)
        {
            string line = KeywordEditBox.Text;
            if (!Keywords.Contains(line))
            {
                Keywords.Add(line);
                KeywordEditBox.Text = "";
                UpdatePreview(line, null);
            }
            else
            {
                KeywordEditBox.Focus();
            }
        }

        private void UpdatePreview(string add, string remove)
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
    }
}
