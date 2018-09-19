using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            //threads.Add(new { Title = "fdjsk", Author = "djdfksdj", Time = "fdsfdsafds", Url = "fdsjkj" });
            listView.DataContext = threads;
            loadThreads();
        }

        private void loadThreads()
        {
            string metaPath = "E:/turboc/sis/meta.json";
            using (StreamReader sr = new StreamReader(metaPath, Encoding.UTF8))
            {
                string json = sr.ReadToEnd();
                List<BBSThread> l = JsonConvert.DeserializeObject<List<BBSThread>>(json);
                l.ForEach(x => threads.Add(new { Title = x.title, Author = x.author, Time = x.postTime, Url = x.link }));
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            //threads.Add(new { Title = "fdjsk", Author = "djdfksdj", Time = "fdsfdsafds", Url = "fdsjkj" });
        }

        public ObservableCollection<object> threads;

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
