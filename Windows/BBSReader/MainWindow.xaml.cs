using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
            currentKeyword = null;
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
            internal bool IsAnthology;
            internal string AnthologyKey;
        }

        private void ResetList(bool simpleBack=false)
        {
            if (currentKeyword == null && currentAnthology == null)
            {
                bool backButtonEnabled = false;
                if (!simpleBack)
                {
                    List<string> tags = new List<string>(metaData.tags.Keys);
                    List<string> anthologies = new List<string>(metaData.anthologies.Keys);

                    if (searchingKeyword != null)
                    {
                        backButtonEnabled = true;

                        tags.RemoveAll(x => {
                            if (x.Contains(searchingKeyword))
                                return false;
                            BBSThread exampleX = metaData.threads[metaData.tags[x][0]];
                            return !exampleX.author.Contains(searchingKeyword);
                        });
                        anthologies.RemoveAll(x => !x.Contains(searchingKeyword));
                    }

                    var items = new List<ListItem>();

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
                        item.Favorite = metaData.favorites.Contains(x);
                        item.IsAnthology = false;
                        item.AnthologyKey = "";

                        items.Add(item);
                    });

                    anthologies.ForEach(x =>
                    {
                        int i = x.IndexOf(':');
                        string author = x.Substring(0, i);
                        string keyword = x.Substring(i + 1);

                        var item = new ListItem();
                        item.Title = keyword == "*" ? ("【" + author + "】的作品集") : keyword;

                        BBSThread example = metaData.threads[metaData.anthologies[x][0]];
                        item.Author = example.author;
                        item.Time = example.postTime;
                        item.Url = example.link;
                        item.Source = "";
                        item.SiteId = example.siteId;
                        item.ThreadId = example.threadId;
                        item.Favorite = true;
                        item.IsAnthology = true;
                        item.AnthologyKey = x;

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
                        return DateTime.Compare(ydate, xdate);
                    });

                    topics.Clear();
                    items.ForEach(x =>
                    {
                        topics.Add(new { x.Title, x.Author, x.Time, x.Url, x.ThreadId, x.Source, x.SiteId, x.Favorite, x.IsAnthology, x.AnthologyKey });
                    });
                }

                TopicList.Visibility = Visibility.Visible;
                ArticleList.Visibility = Visibility.Hidden;
                BackButton.IsEnabled = backButtonEnabled;
            }
            else
            {
                List<int> list;
                if (currentKeyword != null)
                {
                    list = metaData.tags[currentKeyword];
                }
                else if (currentAnthology != null)
                {
                    list = metaData.anthologies[currentAnthology];
                }
                else
                {
                    return;
                }

                articles.Clear();
                list.ForEach(x => {
                    BBSThread t = metaData.threads[x];
                    if (searchingKeyword == null || t.title.Contains(searchingKeyword))
                    {
                        articles.Add(new { Title = t.title, Author = t.author, Time = t.postTime, Url = t.link, ThreadId = t.threadId, Source = SITE_DEF[t.siteId].siteName, SiteId = t.siteId, Favorite = false, IsAnthology = false, AnthologyKey = "" });
                    }
                });

                TopicList.Visibility = Visibility.Hidden;
                ArticleList.Visibility = Visibility.Visible;

                BackButton.IsEnabled = true;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            currentKeyword = null;
            currentAnthology = null;
            if (TopicList.IsVisible)
                SearchBox.Clear();
            ResetList(!TopicList.IsVisible);
        }

        private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem lvi = sender as ListViewItem;
            dynamic item = lvi.Content;
            if (currentKeyword == null && currentAnthology == null)
            {
                if (item.IsAnthology)
                {
                    currentAnthology = item.AnthologyKey;
                }
                else
                {
                    currentKeyword = item.Title;
                }
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
        private string currentKeyword;
        private string currentAnthology;
        private string searchingKeyword;

        public struct MetaData
        {
            [JsonProperty("timestamp")]
            public long timestamp;
            [JsonProperty("threads")]
            public List<BBSThread> threads;
            [JsonProperty("tags")]
            public Dictionary<string, List<int>> tags;
            [JsonProperty("anthologies")]
            public Dictionary<string, List<int>> anthologies;
            [JsonProperty("favorites")]
            public List<string> favorites;
            [JsonProperty("blacklist")]
            public List<string> blacklist;
            [JsonProperty("followings")]
            public Dictionary<string, string> followings;
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
            metaData.favorites.Add(title);
            SaveMetaData();
            currentKeyword = null;
            ResetList();
        }

        private void RemoveFavoritesContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string title = item.Title;
            metaData.favorites.Remove(title);
            SaveMetaData();
            currentKeyword = null;
            ResetList();
        }

        private void BlackContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string title = item.Title;
            metaData.blacklist.Add(title);
            metaData.tags.Remove(title);
            SaveMetaData();
            currentKeyword = null;
            ResetList();
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
                searchingKeyword = null;
            else
                searchingKeyword = SearchBox.Text;
            ResetList();
        }

        private void FollowContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string title = item.Title;
            string author = item.Author;

            var dialog = new FollowDialog();
            dialog.Owner = this;
            dialog.Keyword = title;
            dialog.Author = author;
            var dr = dialog.ShowDialog() ?? false;
            if (dr)
            {
                string keyword = dialog.Keyword;
                metaData.followings[author] = keyword;
                SaveMetaData();
                ResetList();
            }
        }

        private void UnfollowContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string author = item.Author;
            string anthologyKey = item.AnthologyKey;
            metaData.followings.Remove(author);
            metaData.anthologies.Remove(anthologyKey);
            SaveMetaData();
            ResetList();
        }

        private void ViewUrlContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string siteId = item.SiteId;
            string link = item.Url;

            Process.Start(SITE_DEF[siteId].siteHost + link);
        }
    }
}
