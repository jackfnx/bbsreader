using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        const string LOCAL_PATH = "E:/turboc/sis001/";

        public MainWindow()
        {
            InitializeComponent();

            readerWindow = new ReaderWindow();

            listData = new ObservableCollection<object>();
            listView.DataContext = listData;

            LoadMetaData();
            SetCurrentKeyword(null);
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

        private void SetCurrentKeyword(string keyword)
        {
            currentKeyword = keyword;

            listData.Clear();
            if (keyword == null)
            {
                List<string> tags = new List<string>(metaData.tags.Keys);

                tags.Sort((x, y) => {
                    if (metaData.favorites.Contains(x) && !metaData.favorites.Contains(y))
                    {
                        return -1;
                    }
                    else if (!metaData.favorites.Contains(x) && metaData.favorites.Contains(y))
                    {
                        return 1;
                    }
                    return string.Compare(x, y);
                });

                tags.ForEach(x => {
                    BBSThread example = metaData.threads[metaData.tags[x][0]];
                    listData.Add(new
                    {
                        Title = x,
                        Author = example.author,
                        Time = ConvertTimestampToDateString(example.postTime),
                        Url = "",
                        ThreadId = "",
                        Favorite = metaData.favorites.Contains(x)
                    });
                });
            }
            else
            {
                metaData.tags[keyword].ForEach(x => {
                    BBSThread t = metaData.threads[x];
                    listData.Add(new { Title = t.title, Author = t.author, Time = ConvertTimestampToDateString(t.postTime), Url = t.link, ThreadId = t.threadId, Favorite = false });
                });
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentKeyword(null);
        }

        private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem lvi = sender as ListViewItem;
            dynamic item = lvi.Content;
            if (currentKeyword == null)
            {
                SetCurrentKeyword(item.Title);
            }
            else
            {
                string fPath = LOCAL_PATH + item.ThreadId + ".txt";
                if (File.Exists(fPath))
                {
                    using (StreamReader sr = new StreamReader(fPath, new UTF8Encoding(false)))
                    {
                        string text = sr.ReadToEnd();
                        readerWindow.content.Text = text;
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
        public ObservableCollection<object> listData;
        private MetaData metaData;
        private string currentKeyword;

        public struct MetaData
        {
            [JsonProperty("timestamp")]
            public long timestamp;
            [JsonProperty("threads")]
            public List<BBSThread> threads;
            [JsonProperty("tags")]
            public Dictionary<string, List<int>> tags;
            [JsonProperty("favorites")]
            public List<string> favorites;
            [JsonProperty("blacklist")]
            public List<string> blacklist;
        }

        public struct BBSThread
        {
            [JsonProperty("threadId")]
            public string threadId;
            [JsonProperty("title")]
            public string title;
            [JsonProperty("author")]
            public string author;
            [JsonProperty("postTime")]
            public long postTime;
            [JsonProperty("link")]
            public string link;
        }

        private void AddFavoritesContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string title = item.Title;
            metaData.favorites.Add(title);
            SaveMetaData();
            SetCurrentKeyword(null);
        }

        private void RemoveFavoritesContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string title = item.Title;
            metaData.favorites.Remove(title);
            SaveMetaData();
            SetCurrentKeyword(null);
        }

        private void BlackContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string title = item.Title;
            metaData.blacklist.Add(title);
            metaData.tags.Remove(title);
            SaveMetaData();
            SetCurrentKeyword(null);
        }

        private string ConvertTimestampToDateString(long timestamp)
        {
            DateTime zero = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            TimeSpan span = new TimeSpan(timestamp * 10000000);
            DateTime t = zero.Add(span);
            return t.ToString("yyyy-MM-dd");
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
