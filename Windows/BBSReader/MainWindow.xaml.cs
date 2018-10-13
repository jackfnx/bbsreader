using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BBSReader
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        const string LOCAL_PATH = "C:/Users/hpjing/Dropbox/BBSReader.Cache/";
        static readonly Dictionary<string, BBSDef> SITE_DEF = new Dictionary<string, BBSDef> {
            { "sis001", new BBSDef { siteId="sis001", siteName="第一会所", siteHost="http://www.sis001.com/forum/" } },
            { "sexinsex", new BBSDef { siteId="sexinsex", siteName="色中色", siteHost="http://www.sexinsex.net/bbs/" } }
        };

        //private ReaderWindow readerWindow;
        public ObservableCollection<object> topics;
        public ObservableCollection<object> articles;
        private MetaData metaData;
        private int currentId;
        private string searchingKeyword;
        private string text;
        private AppState currentState;

        public struct BBSDef
        {
            public string siteId;
            public string siteName;
            public string siteHost;
        }

        public enum AppState
        {
            TOPICS,
            ARTICLES,
            READER
        }

        public struct MetaData
        {
            [JsonProperty("timestamp")]
            public long timestamp;
            [JsonProperty("threads")]
            public List<BBSThread> threads;
            [JsonProperty("tags")]
            public Dictionary<string, List<int>> tags;
            [JsonProperty("superkeywords")]
            public List<SuperKeyword> superKeywords;
            [JsonProperty("blacklist")]
            public List<string> blacklist;
        }

        public struct BBSThread
        {
            [JsonProperty("siteId")]
            public string siteId;
            [JsonProperty("threadId")]
            public string threadId;
            [JsonProperty("title")]
            public string title;
            [JsonProperty("author")]
            public string author;
            [JsonProperty("postTime")]
            public string postTime;
            [JsonProperty("link")]
            public string link;
        }

        public struct SuperKeyword
        {
            [JsonProperty("simple")]
            public bool simple;
            [JsonProperty("keyword")]
            public string keyword;
            [JsonProperty("author")]
            public List<string> authors;
            [JsonProperty("tids")]
            public List<int> tids;
            [JsonProperty("read")]
            public int read;
            [JsonProperty("groups")]
            public List<List<string>> groups;
            [JsonIgnore]
            public List<Group> groupedTids;
        }

        public struct Group
        {
            public int exampleId;
            public string tooltips;
        }

        private struct ListItem
        {
            internal string Title;
            internal string Author;
            internal string Time;
            internal string Url;
            internal string Source;
            internal string SiteId;
            internal string ThreadId;
            internal bool Favorite;
            internal bool Simple;
            internal int FavoriteId;
            internal bool IsFolder;
            internal bool Downloaded;
            internal bool Read;
            internal string Tooltip;
        }

        public MainWindow()
        {
            InitializeComponent();
            
            LoadMetaData();

            topics = new ObservableCollection<object>();
            TopicListView.DataContext = topics;

            articles = new ObservableCollection<object>();
            ArticleListView.DataContext = articles;

            searchingKeyword = null;
            ReloadTopics();

            currentState = AppState.TOPICS;
            currentId = -1;

            text = "";
            UpdateView();
        }

        private void LoadMetaData()
        {
            string metaPath = LOCAL_PATH + "meta.json";
            using (StreamReader sr = new StreamReader(metaPath, Encoding.UTF8))
            {
                string json = sr.ReadToEnd();
                metaData = JsonConvert.DeserializeObject<MetaData>(json);
                GroupingSuperKeyword();
            }
        }

        private void SaveMetaData()
        {
            string metaPath = LOCAL_PATH + "meta.json";
            using (StreamWriter sw = new StreamWriter(metaPath, false, new UTF8Encoding(false)))
            {
                string json = JsonConvert.SerializeObject(metaData);
                sw.Write(json);
            }
        }

        private List<T> in_group<T>(T tid, List<List<T>> groups)
        {
            foreach (var group in groups)
            {
                if (group.Contains(tid))
                    return group;
            }
            return null;
        }

        private void GroupingSuperKeyword()
        {
            for (int i = 0; i < metaData.superKeywords.Count(); i++)
            {
                var sk = metaData.superKeywords[i];
                var tids = sk.tids;
                var groups = sk.groups;
                List<string> keys = new List<string>();
                foreach (int tid in tids)
                {
                    var t = metaData.threads[tid];
                    string key = t.siteId + "/" + t.threadId;
                    keys.Add(key);
                }
                List<List<int>> id_groups = new List<List<int>>();
                for (int j = 0; j < tids.Count; j++)
                {
                    int tid = tids[j];
                    string key = keys[j];
                    if (in_group(tid, id_groups) != null)
                        continue;
                    List<int> id_group = new List<int> { tid };
                    List<string> key_group = in_group(key, groups);
                    if (key_group != null)
                    {
                        foreach (var anotherKey in key_group)
                        {
                            if (anotherKey != key)
                            {
                                int k = keys.IndexOf(anotherKey);
                                id_group.Add(tids[k]);
                            }
                        }
                    }
                    id_groups.Add(id_group);
                }
                sk.groupedTids = new List<Group>();
                var siteIds = new List<string>(SITE_DEF.Keys);
                foreach (var id_group in id_groups)
                {
                    id_group.Sort((x, y) => siteIds.IndexOf(metaData.threads[x].siteId) - siteIds.IndexOf(metaData.threads[y].siteId));
                    List<string> lines = id_group.ConvertAll(x =>
                    {
                        var t = metaData.threads[x];
                        string siteName = SITE_DEF[t.siteId].siteName;
                        return string.Format("[{0}]\t<{1}>\t{2}", t.title, t.author, siteName);
                    });
                    sk.groupedTids.Add(new Group { exampleId = id_group[0], tooltips = string.Join("\n", lines)});
                }
                metaData.superKeywords[i] = sk;
            }
        }

        private void UpdateView()
        {
            if (currentState == AppState.TOPICS)
            {
                TopicListView.Visibility = Visibility.Visible;
                ArticleListView.Visibility = Visibility.Hidden;
                ReaderView.Visibility = Visibility.Hidden;
                //TopicList.Focus();
            }
            else if (currentState == AppState.ARTICLES)
            {
                TopicListView.Visibility = Visibility.Hidden;
                ArticleListView.Visibility = Visibility.Visible;
                ReaderView.Visibility = Visibility.Hidden;
                //ArticleList.Focus();
            }
            else if (currentState == AppState.READER)
            {
                TopicListView.Visibility = Visibility.Hidden;
                ArticleListView.Visibility = Visibility.Hidden;
                ReaderView.Visibility = Visibility.Visible;
                //ReaderView.Focus();
            }

            BackButton.IsEnabled = currentState != AppState.TOPICS || searchingKeyword != null;
        }

        private void ReloadTopics()
        {
            List<string> tags = new List<string>(metaData.tags.Keys);
            var superkeywords = new List<SuperKeyword>(metaData.superKeywords);

            if (searchingKeyword != null)
            {
                tags.RemoveAll(x =>
                {
                    if (x.Contains(searchingKeyword))
                        return false;
                    BBSThread exampleX = metaData.threads[metaData.tags[x][0]];
                    return !exampleX.author.Contains(searchingKeyword);
                });
                superkeywords.RemoveAll(x =>
                {
                    if (x.keyword.Contains(searchingKeyword))
                        return false;
                    foreach (string author in x.authors)
                    {
                        if (author.Contains(searchingKeyword))
                            return false;
                    }
                    return true;
                });
            }

            List<ListItem> items = new List<ListItem>();

            tags.ForEach(x =>
            {
                var item = new ListItem();
                item.Title = x;

                BBSThread example = metaData.threads[metaData.tags[x][0]];
                item.Author = example.author;
                item.Time = example.postTime;
                item.Url = example.link;
                item.Source = "";
                item.SiteId = example.siteId;
                item.ThreadId = example.threadId;
                item.Favorite = false;
                item.Simple = true;
                item.FavoriteId = -1;
                item.IsFolder = true;
                item.Downloaded = false;
                item.Read = true;

                items.Add(item);
            });

            superkeywords.ForEach(x =>
            {
                string author = x.authors[0];
                string keyword = x.keyword;

                var item = new ListItem();

                if (x.simple)
                    item.Title = x.keyword;
                else if (keyword == "*")
                    item.Title = ("【" + author + "】的作品集");
                else if (author == "*")
                    item.Title = ("专题：【" + keyword + "】");
                else
                    item.Title = ("【" + keyword + "】系列");
                item.Author = author;

                BBSThread example = metaData.threads[x.groupedTids[0].exampleId];
                item.Time = example.postTime;
                item.Url = example.link;
                item.Source = "";
                item.SiteId = example.siteId;
                item.ThreadId = example.threadId;
                item.Favorite = true;
                item.Simple = x.simple;
                item.FavoriteId = metaData.superKeywords.IndexOf(x);
                item.IsFolder = true;
                item.Downloaded = false;
                item.Read = x.groupedTids.Count <= x.read + 1;

                items.Add(item);
            });

            items.Sort((x, y) =>
            {
                if (x.Favorite && !y.Favorite)
                {
                    return -1;
                }
                else if (!x.Favorite && y.Favorite)
                {
                    return 1;
                }
                DateTime xdate = DateTime.ParseExact(x.Time, "yyyy-M-d", CultureInfo.InvariantCulture);
                DateTime ydate = DateTime.ParseExact(y.Time, "yyyy-M-d", CultureInfo.InvariantCulture);
                int dc = DateTime.Compare(ydate, xdate);
                if (dc != 0)
                    return dc;
                else
                    return int.Parse(y.ThreadId) - int.Parse(x.ThreadId);
            });

            topics.Clear();
            items.ForEach(x =>
            {
                topics.Add(new { x.Title, x.Author, x.Time, x.Url, x.ThreadId, x.Source, x.SiteId, x.Favorite, x.Simple, x.FavoriteId, x.IsFolder, x.Downloaded, x.Read, x.Tooltip });
            });
        }

        private void ReloadArticles()
        {
            dynamic item = topics[currentId];
            List<Group> list;
            int read;
            if (item.Favorite)
            {
                SuperKeyword sk = metaData.superKeywords[item.FavoriteId];
                list = sk.groupedTids;
                read = sk.read;
            }
            else
            {
                string title = item.Title;
                list = metaData.tags[title].ConvertAll(x => new Group { exampleId = x, tooltips = "" });
                read = list.Count;
            }

            articles.Clear();
            list.ForEach(x =>
            {
                int i = list.Count - list.IndexOf(x) - 1;
                BBSThread t = metaData.threads[x.exampleId];
                if (searchingKeyword == null || t.title.Contains(searchingKeyword))
                {
                    string siteName = SITE_DEF[t.siteId].siteName;
                    string fPath = LOCAL_PATH + t.siteId + "/" + t.threadId + ".txt";
                    bool downloaded = File.Exists(fPath);
                    articles.Add(new
                    {
                        Title = t.title,
                        Author = t.author,
                        Time = t.postTime,
                        Url = t.link,
                        ThreadId = t.threadId,
                        Source = siteName,
                        SiteId = t.siteId,
                        Favorite = false,
                        Simple = true,
                        FavoriteId = -1,
                        IsFolder = false,
                        Downloaded = downloaded,
                        Read = i <= read,
                        Tooltip = x.tooltips
                    });
                }
            });
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Backward();
        }

        private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem lvi = sender as ListViewItem;
            dynamic item = lvi.Content;
            if (currentState == AppState.TOPICS)
            {
                ForwardAtTopics(item);
            }
            else if (currentState == AppState.ARTICLES)
            {
                ForwardAtArticles(item);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
                searchingKeyword = null;
            else
                searchingKeyword = SearchBox.Text;

            if (currentState == AppState.TOPICS)
                ReloadTopics();
            else if (currentState == AppState.ARTICLES)
                ReloadArticles();
            UpdateView();
        }

        private void ForwardAtTopics(dynamic item)
        {
            currentId = topics.IndexOf(item);
            currentState = AppState.ARTICLES;
            ReloadArticles();
            UpdateView();
        }

        private void Backward()
        {
            if (currentState == AppState.READER)
                currentState = AppState.ARTICLES;
            else if (currentState == AppState.ARTICLES)
                currentState = AppState.TOPICS;
            else if (currentState == AppState.TOPICS && searchingKeyword != null)
                SearchBox.Clear();
            UpdateView();
        }

        private void ForwardAtArticles(dynamic item)
        {
            string fPath = LOCAL_PATH + item.SiteId + "/" + item.ThreadId + ".txt";
            if (File.Exists(fPath))
            {
                using (StreamReader sr = new StreamReader(fPath, new UTF8Encoding(false)))
                {
                    text = sr.ReadToEnd();
                    currentState = AppState.READER;
                    ReaderText.Text = text;
                    ReaderScroll.ScrollToHome();
                    UpdateView();
                }

                dynamic topic = topics[currentId];
                int favoriteId = topic.FavoriteId;
                int index = articles.IndexOf(item);
                var sk = metaData.superKeywords[favoriteId];
                int i = sk.groupedTids.Count - index - 1;
                if (i > sk.read)
                {
                    sk.read = i;
                    metaData.superKeywords[favoriteId] = sk;
                    SaveMetaData();
                    ReloadArticles();
                    ReloadTopics();
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Space)
            {
                if (currentState == AppState.READER)
                {
                    ReaderScroll.PageDown();
                    ReaderScroll.LineUp();
                    ReaderScroll.LineUp();
                    e.Handled = true;
                }
                else if (currentState == AppState.ARTICLES)
                {
                    int i = ArticleListView.SelectedIndex;
                    if (i < 0)
                    {
                        ArticleListView.SelectedIndex = 0;
                    }
                    //Keyboard.Focus(ArticleListView.SelectedItem as ListViewItem);
                }
                else if (currentState == AppState.TOPICS)
                {
                    int i = TopicListView.SelectedIndex;
                    if (i < 0)
                    {
                        TopicListView.SelectedIndex = 0;
                    }
                    //Keyboard.Focus(TopicListView.SelectedItem as ListViewItem);
                }
            }
            else if (e.Key == Key.Right || e.Key == Key.Enter || e.Key == Key.R)
            {
                if (currentState == AppState.TOPICS)
                {
                    dynamic item = TopicListView.SelectedItem;
                    if (item == null)
                        TopicListView.SelectedIndex = 0;
                    item = TopicListView.SelectedItem;
                    if (item != null)
                        ForwardAtTopics(item);
                }
                else if (currentState == AppState.ARTICLES)
                {
                    dynamic item = ArticleListView.SelectedItem;
                    if (item == null)
                        item = ArticleListView.SelectedItem;
                    if (item != null)
                        ForwardAtArticles(item);
                }
            }
            else if (e.Key == Key.Left || e.Key == Key.E || e.Key == Key.Q)
            {
                Backward();
            }
            //else if (e.Key == Key.Up)
            //{
            //    if (currentState == AppState.TOPICS)
            //    {
            //        int i = TopicListView.SelectedIndex;
            //        if ((i + 1) < TopicListView.Items.Count)
            //        {
            //            TopicListView.SelectedIndex = i + 1;
            //        }
            //    }
            //    else if (currentState == AppState.ARTICLES)
            //    {
            //        int i = ArticleListView.SelectedIndex;
            //        if ((i - 1) < ArticleListView.Items.Count)
            //        {
            //            ArticleListView.SelectedIndex = i + 1;
            //        }
            //    }
            //}
            //else if (e.Key == Key.Down)
            //{
            //    if (currentState == AppState.TOPICS)
            //    {
            //        int i = TopicListView.SelectedIndex;
            //        if (i > 0)
            //        {
            //            TopicListView.SelectedIndex = i - 1;
            //        }
            //    }
            //    else if (currentState == AppState.ARTICLES)
            //    {
            //        int i = ArticleListView.SelectedIndex;
            //        if (i > 0)
            //        {
            //            ArticleListView.SelectedIndex = i - 1;
            //        }
            //    }
            //}
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ResetFont();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResetFont();
        }

        private void ResetFont()
        {
            string rulerText = new string('女', 40);
            for (double fontSize = 9; fontSize < 60; fontSize += 1)
            {
                var formattedText = new FormattedText(rulerText,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(ReaderText.FontFamily, ReaderText.FontStyle, ReaderText.FontWeight, ReaderText.FontStretch),
                    fontSize,
                    Brushes.Black,
                    new NumberSubstitution());
                if (formattedText.Width >= (this.ActualWidth * 35 / 40))
                {
                    break;
                }
                ReaderText.Width = formattedText.Width;
                ReaderText.FontSize = fontSize;
            }
        }

        private void AddFavoritesContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string title = item.Title;
            if (metaData.tags.ContainsKey(title))
            {
                var tids = metaData.tags[title];
                metaData.tags.Remove(title);
                SuperKeyword superKeyword = new SuperKeyword();
                superKeyword.simple = true;
                superKeyword.keyword = title;
                superKeyword.authors = new List<string>{ item.Author };
                superKeyword.tids = tids;
                superKeyword.read = -1;
                superKeyword.groups = new List<List<string>>();
                superKeyword.groupedTids = tids.ConvertAll(x => new Group { exampleId = x, tooltips = "" });
                metaData.superKeywords.Add(superKeyword);
                SaveMetaData();
                currentId = -1;
                currentState = AppState.TOPICS;
                ReloadTopics();
                UpdateView();
            }
        }

        private void RemoveFavoritesContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string title = item.Title;
            int FavoriteId = item.FavoriteId;
            SuperKeyword superKeyword = metaData.superKeywords[FavoriteId];
            if (superKeyword.simple)
            {
                var tids = superKeyword.tids;
                var keyword = superKeyword.keyword;
                metaData.superKeywords.Remove(superKeyword);
                metaData.tags[keyword] = tids;
                SaveMetaData();
                currentId = -1;
                currentState = AppState.TOPICS;
                ReloadTopics();
                UpdateView();
            }
        }

        private void BlackContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string title = item.Title;
            if (item.Simple)
            {
                metaData.blacklist.Add(title);
                metaData.tags.Remove(title);
                SaveMetaData();
                currentId = -1;
                currentState = AppState.TOPICS;
                ReloadTopics();
                UpdateView();
            }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadDialog dialog = new DownloadDialog();
            if (dialog.ShowDialog() ?? false)
            {
                LoadMetaData();
                currentId = -1;
                searchingKeyword = null;
                currentState = AppState.TOPICS;
                ReloadTopics();
                UpdateView();
            }
        }

        private void SetAdvancedKeywordContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string title = item.Title;
            string author = item.Author;

            var dialog = new AdvancedKeywordDialog();
            dialog.Owner = this;
            dialog.Keyword = title;
            dialog.DefaultAuthor = author;
            var dr = dialog.ShowDialog() ?? false;
            if (dr)
            {
                SuperKeyword superKeyword = new SuperKeyword();
                superKeyword.simple = false;
                superKeyword.keyword = dialog.Keyword;
                superKeyword.authors = new List<string>(dialog.AuthorList);
                superKeyword.tids = new List<int>();
                superKeyword.read = -1;
                superKeyword.groups = new List<List<string>>();
                superKeyword.groupedTids = new List<Group>();
                if (!metaData.superKeywords.Contains(superKeyword))
                {
                    metaData.superKeywords.Add(superKeyword);
                    SaveMetaData();
                    currentState = AppState.TOPICS;
                    ReloadTopics();
                    UpdateView();
                }
            }
        }

        private void CancelAdvancedKeywordContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            if (!item.Simple)
            {
                int FavoriteId = item.FavoriteId;
                metaData.superKeywords.RemoveAt(FavoriteId);
                SaveMetaData();
                currentState = AppState.TOPICS;
                ReloadTopics();
                UpdateView();
            }
        }

        private void ViewUrlContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string siteId = item.SiteId;
            string link = item.Url;

            Process.Start(SITE_DEF[siteId].siteHost + link);
        }

        private void DeleteTxtContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string siteId = item.SiteId;
            string threadId = item.ThreadId;

            string fPath = LOCAL_PATH + item.SiteId + "/" + item.ThreadId + ".txt";
            File.Delete(fPath);
            if (currentState == AppState.ARTICLES)
                ReloadArticles();
        }

        private void UnreadContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            if (!item.IsFolder)
            {
                dynamic topic = topics[currentId];
                if (topic.Favorite)
                {
                    int favoriteId = topic.FavoriteId;
                    int index = articles.IndexOf(item) + 1;
                    var sk = metaData.superKeywords[favoriteId];
                    int i = sk.groupedTids.Count - index - 1;
                    if (i <= sk.read)
                    {
                        sk.read = i;
                        metaData.superKeywords[favoriteId] = sk;
                        SaveMetaData();
                        ReloadArticles();
                        ReloadTopics();
                        UpdateView();
                    }
                }
            }
        }
    }
}
