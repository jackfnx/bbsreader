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

            threads = new ObservableCollection<object>();
            listView.DataContext = threads;

            loadMetaData();
            setCurrentKeyword(null);
        }

        private void loadMetaData()
        {
            string metaPath = LOCAL_PATH + "meta.json";
            using (StreamReader sr = new StreamReader(metaPath, Encoding.UTF8))
            {
                string json = sr.ReadToEnd();
                metaData = JsonConvert.DeserializeObject<MetaData>(json);
            }
        }

        private void setCurrentKeyword(string keyword)
        {
            currentKeyword = keyword;

            threads.Clear();
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
                tags.ForEach(x => threads.Add(new { Title = x, Author = "-", Time = "", Url = "", ThreadId = "", Favorite = metaData.favorites.Contains(x) }));
            }
            else
            {
                metaData.tags[keyword].ForEach(x => threads.Add(new { Title = x.title, Author = x.author, Time = x.postTime, Url = x.link, ThreadId = x.threadId, Favorite = false }));
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            setCurrentKeyword(null);
        }

        private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem lvi = sender as ListViewItem;
            dynamic item = lvi.Content;
            if (currentKeyword == null)
            {
                setCurrentKeyword(item.Title);
            }
            else
            {
                string fPath = LOCAL_PATH + item.ThreadId + ".txt";
                using (StreamReader sr = new StreamReader(fPath, Encoding.UTF8))
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

        protected override void OnClosed(EventArgs e)
        {
            readerWindow.Stay = false;
            readerWindow.Close();
            base.OnClosed(e);
        }
        
        private ReaderWindow readerWindow;
        public ObservableCollection<object> threads;
        private MetaData metaData;
        private string currentKeyword;

        public struct MetaData
        {
            [JsonProperty("timestamp")]
            public long timestamp;
            [JsonProperty("threads")]
            public List<BBSThread> threads;
            [JsonProperty("tags")]
            public Dictionary<string, List<BBSThread>> tags;
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
            public string postTime;
            [JsonProperty("link")]
            public string link;
        }
    }
}
