using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ForumFetcher
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            threads = new ObservableCollection<object>();
            listView.DataContext = threads;

            savePath = "E:/turboc/sis001/";
            loadThreads();
            setCurrentKeyword(null);
        }

        private void setCurrentKeyword(string keyword)
        {
            currentKeyword = keyword;

            threads.Clear();
            if (keyword == null)
            {
                List<string> tags = new List<string>(saveData.tags.Keys);
                tags.Sort((x, y) => {
                    if (saveData.favorites.Contains(x) && !saveData.favorites.Contains(y))
                    {
                        return -1;
                    }
                    else if (!saveData.favorites.Contains(x) && saveData.favorites.Contains(y))
                    {
                        return 1;
                    }
                    return string.Compare(x, y);
                });
                tags.ForEach(x => threads.Add(new { Title = x, Author = "-", Time = "", Url = "", ThreadId = "", Favorite = saveData.favorites.Contains(x) }));
            }
            else
            {
                saveData.tags[keyword].ForEach(x => threads.Add(new { Title = x.title, Author = x.author, Time = x.postTime, Url = x.link, ThreadId = x.threadId, Favorite = false }));
            }
        }

        private void loadThreads()
        {
            string metaPath = savePath + "meta.json";
            using (StreamReader sr = new StreamReader(metaPath, Encoding.UTF8))
            {
                string json = sr.ReadToEnd();
                saveData = JsonConvert.DeserializeObject<SAVE_DATA>(json);
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
                string fPath = savePath + item.ThreadId + ".txt";
                using (StreamReader sr = new StreamReader(fPath, Encoding.UTF8))
                {
                    string text = sr.ReadToEnd();
                    Console.WriteLine(text);
                }
            }
        }

        public ObservableCollection<object> threads;
        private string savePath;
        private SAVE_DATA saveData;
        private string currentKeyword;

        public struct SAVE_DATA
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
