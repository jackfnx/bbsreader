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
        const string LOCAL_PATH = "E:/BBSReader.Cache/sis001/";

        public MainWindow()
        {
            InitializeComponent();

            readerWindow = new ReaderWindow();

            listData = new ObservableCollection<object>();
            listView.DataContext = listData;

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

        private void ResetList()
        {
            listData.Clear();
            if (currentKeyword == null)
            {
                List<string> tags = new List<string>(metaData.tags.Keys);

                if (searchingKeyword != null)
                {
                    tags.RemoveAll(x => { return !x.Contains(searchingKeyword); });
                }

                tags.Sort((x, y) => {
                    if (metaData.favorites.Contains(x) && !metaData.favorites.Contains(y))
                    {
                        return -1;
                    }
                    else if (!metaData.favorites.Contains(x) && metaData.favorites.Contains(y))
                    {
                        return 1;
                    }
                    BBSThread exampleX = metaData.threads[metaData.tags[x][0]];
                    BBSThread exampleY = metaData.threads[metaData.tags[y][0]];
                    return int.Parse(exampleY.threadId) - int.Parse(exampleX.threadId);
                });

                tags.ForEach(x => {
                    BBSThread example = metaData.threads[metaData.tags[x][0]];
                    listData.Add(new
                    {
                        Title = x,
                        Author = example.author,
                        Time = example.postTime,
                        Url = "",
                        ThreadId = "",
                        Favorite = metaData.favorites.Contains(x)
                    });
                });
            }
            else
            {
                metaData.tags[currentKeyword].ForEach(x => {
                    BBSThread t = metaData.threads[x];
                    listData.Add(new { Title = t.title, Author = t.author, Time = t.postTime, Url = t.link, ThreadId = t.threadId, Favorite = false });
                });
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            currentKeyword = null;
            ResetList();
        }

        private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem lvi = sender as ListViewItem;
            dynamic item = lvi.Content;
            if (currentKeyword == null)
            {
                currentKeyword = item.Title;
                ResetList();
            }
            else
            {
                string fPath = LOCAL_PATH + item.ThreadId + ".txt";
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
        public ObservableCollection<object> listData;
        private MetaData metaData;
        private string currentKeyword;
        private string searchingKeyword;

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
            public string postTime;
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

        private void KeywordButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
                searchingKeyword = null;
            else
                searchingKeyword = SearchTextBox.Text;
            ResetList();
        }
    }
}
