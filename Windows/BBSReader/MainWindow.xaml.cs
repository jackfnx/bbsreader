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

        public MainWindow()
        {
            InitializeComponent();

            readerWindow = new ReaderWindow();

            topics = new ObservableCollection<object>();
            TopicList.DataContext = topics;

            articles = new ObservableCollection<object>();
            ArticleList.DataContext = articles;

            LoadMetaData();
            currentId = -1;
            searchingKeyword = null;
            ResetList();
        }

        private void LoadMetaData()
        {
            string metaPath = LOCAL_PATH + "meta.json";
            using (StreamReader sr = new StreamReader(metaPath, Encoding.UTF8))
            {
                string json = sr.ReadToEnd();
                metaData = JsonConvert.DeserializeObject<MetaData>(json);
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
        }

        private void ResetList(bool simpleBack=false)
        {
            if (currentId < 0)
            {
                bool backButtonEnabled = false;
                if (!simpleBack)
                {
                    List<string> tags = new List<string>(metaData.tags.Keys);
                    var superkeywords = new List<SuperKeyword>(metaData.superKeywords);

                    if (searchingKeyword != null)
                    {
                        backButtonEnabled = true;

                        tags.RemoveAll(x => {
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

                        BBSThread example = metaData.threads[x.tids[0]];
                        item.Time = example.postTime;
                        item.Url = example.link;
                        item.Source = "";
                        item.SiteId = example.siteId;
                        item.ThreadId = example.threadId;
                        item.Favorite = true;
                        item.Simple = x.simple;
                        item.FavoriteId = superkeywords.IndexOf(x);
                        item.IsFolder = true;
                        item.Downloaded = false;
                        item.Read = x.tids.Count <= x.read + 1;

                        items.Add(item);
                    });

                    items.Sort((x, y) => {
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
                        topics.Add(new { x.Title, x.Author, x.Time, x.Url, x.ThreadId, x.Source, x.SiteId, x.Favorite, x.Simple, x.FavoriteId, x.IsFolder, x.Downloaded, x.Read });
                    });
                }

                TopicList.Visibility = Visibility.Visible;
                ArticleList.Visibility = Visibility.Hidden;
                BackButton.IsEnabled = backButtonEnabled;
            }
            else
            {
                dynamic item = topics[currentId];
                List<int> list = item.Favorite ? metaData.superKeywords[item.FavoriteId].tids : metaData.tags[item.Title];
                int read = item.Favorite ? metaData.superKeywords[item.FavoriteId].read : list.Count;

                articles.Clear();
                list.ForEach(x => {
                    int i = list.Count - list.IndexOf(x) - 1;
                    BBSThread t = metaData.threads[x];
                    if (searchingKeyword == null || t.title.Contains(searchingKeyword))
                    {
                        string siteName = SITE_DEF[t.siteId].siteName;
                        string fPath = LOCAL_PATH + t.siteId + "/" + t.threadId + ".txt";
                        bool downloaded = File.Exists(fPath);
                        articles.Add(new {
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
                            Read = i <= read});
                    }
                });

                TopicList.Visibility = Visibility.Hidden;
                ArticleList.Visibility = Visibility.Visible;

                BackButton.IsEnabled = true;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            currentId = -1;
            if (TopicList.IsVisible)
                SearchBox.Clear();
            ResetList(!TopicList.IsVisible);
        }

        private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem lvi = sender as ListViewItem;
            dynamic item = lvi.Content;
            if (item.IsFolder)
            {
                currentId = topics.IndexOf(item);
                ResetList();
            }
            else
            {
                string fPath = LOCAL_PATH + item.SiteId + "/" + item.ThreadId + ".txt";
                if (File.Exists(fPath))
                {
                    using (StreamReader sr = new StreamReader(fPath, new UTF8Encoding(false)))
                    {
                        string text = sr.ReadToEnd();
                        readerWindow.ContentText.Text = text;
                        readerWindow.Scroll.ScrollToHome();
                        if (!readerWindow.IsVisible)
                        {
                            readerWindow.Show();
                        }
                    }

                    dynamic topic = topics[currentId];
                    int favoriteId = topic.FavoriteId;
                    int index = articles.IndexOf(item);
                    var sk = metaData.superKeywords[favoriteId];
                    int i = sk.tids.Count - index - 1;
                    if (i > sk.read)
                    {
                        sk.read = i;
                        metaData.superKeywords[favoriteId] = sk;
                        SaveMetaData();
                        ResetList();
                    }
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            readerWindow.Stay = false;
            readerWindow.Close();
            base.OnClosed(e);
        }
        
        private ReaderWindow readerWindow;
        public ObservableCollection<object> topics;
        public ObservableCollection<object> articles;
        private MetaData metaData;
        private int currentId;
        private string searchingKeyword;

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
            public List<List<int>> groups;
        }

        public struct BBSDef
        {
            public string siteId;
            public string siteName;
            public string siteHost;
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
                superKeyword.groups = new List<List<int>>();
                metaData.superKeywords.Add(superKeyword);
                SaveMetaData();
                currentId = -1;
                ResetList();
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
                ResetList();
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
                ResetList();
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
                ResetList();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
                searchingKeyword = null;
            else
                searchingKeyword = SearchBox.Text;
            ResetList();
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
                superKeyword.groups = new List<List<int>>();
                if (!metaData.superKeywords.Contains(superKeyword))
                {
                    metaData.superKeywords.Add(superKeyword);
                    SaveMetaData();
                    ResetList();
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
                ResetList();
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
            ResetList();
        }

        private void UnreadContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            if (!item.IsFolder)
            {
                dynamic topic = topics[currentId];
                int favoriteId = topic.FavoriteId;
                int index = articles.IndexOf(item) + 1;
                var sk = metaData.superKeywords[favoriteId];
                int i = sk.tids.Count - index - 1;
                if (i <= sk.read)
                {
                    sk.read = i;
                    metaData.superKeywords[favoriteId] = sk;
                    SaveMetaData();
                    ResetList();
                }
            }
        }
    }
}
