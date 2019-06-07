using BBSReader.Data;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace BBSReader
{
    /// <summary>
    /// PacketWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PacketWindow : Window
    {
        private ObservableCollection<string> filenames;
        private ObservableCollection<object> contents;

        public PacketWindow()
        {
            InitializeComponent();

            filenames = new ObservableCollection<string>();
            FileNameListView.DataContext = filenames;

            contents = new ObservableCollection<object>();
            ContentListView.DataContext = contents;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string s = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
                if (string.IsNullOrWhiteSpace(BookTitle.Text))
                {
                    string basename = s.Substring(s.LastIndexOfAny("/\\".ToCharArray()) + 1);
                    basename = basename.Substring(0, basename.LastIndexOf("."));
                    if (basename.IndexOf("_") != -1)
                    {
                        BookTitle.Text = basename.Substring(0, basename.IndexOf("_"));
                        BookAuthor.Text = basename.Substring(basename.IndexOf("_") + 1);
                    }
                    else
                    {
                        BookTitle.Text = basename;
                        BookAuthor.Text = "";
                    }
                }
                if (!filenames.Contains(s))
                {
                    foreach (object ch in SearchChapterNodes(s, filenames.Count))
                    {
                        contents.Add(ch);
                    }
                    filenames.Add(s);
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            BookTitle.Text = "";
            BookAuthor.Text = "";
            filenames.Clear();
            contents.Clear();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            Packet packet = new Packet();
            packet.title = BookTitle.Text;
            packet.author = BookAuthor.Text;
            packet.simple = true;
            packet.chapters = new List<Chapter>();
            foreach (dynamic tch in contents)
            {
                Chapter ch = new Chapter();
                ch.id = tch.id;
                ch.title = tch.title;
                ch.author = BookAuthor.Text;
                ch.source = "TextRepack";
                ch.savePath = ch.id + ".txt";
                ch.timestamp = 0;
                packet.chapters.Add(ch);
            }
            packet.chapters.Reverse();
            packet.timestamp = 0;
            packet.key = Utils.CalcKey(packet.title, packet.author, true);
            packet.summary = Utils.CalcSumary(packet.title, packet.author, true, packet.chapters);
            packet.source = "TextRepack";

            string folder = string.Format("{0}/packets", Constants.LOCAL_PATH);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string path = string.Format("{0}/{1}_{2}.zip", folder, packet.title, packet.author);

            PacketToZip(packet, path, contents);
        }

        private static void PacketToZip(Packet packet, string path, ObservableCollection<object> contents)
        {
            string metaJson = JsonConvert.SerializeObject(packet);
            string contentJson = JsonConvert.SerializeObject(packet.chapters);
            using (FileStream ms = new FileStream(path, FileMode.Create))
            {
                using (ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Create))
                {
                    ZipArchiveEntry meta = zip.CreateEntry(".META.json");
                    using (StreamWriter sw = new StreamWriter(meta.Open()))
                    {
                        sw.Write(metaJson);
                    }
                    ZipArchiveEntry content = zip.CreateEntry(".CONTENT");
                    using (StreamWriter sw = new StreamWriter(content.Open()))
                    {
                        sw.Write(contentJson);
                    }
                    for (int i = 0; i < packet.chapters.Count; i++)
                    {
                        Chapter ch = packet.chapters[packet.chapters.Count - i - 1];
                        dynamic tch = contents[i];
                        ZipArchiveEntry zae = zip.CreateEntry(ch.savePath);
                        using (Stream outs = zae.Open())
                        {
                            byte[] bytes = Encoding.UTF8.GetBytes(tch.content);
                            outs.Write(bytes, 0, bytes.Length);
                        }
                    }
                }
            }
        }

        private static List<object> SearchChapterNodes(string path, int k)
        {
            using (TextReader tr = new StreamReader(path, Encoding.UTF8))
            {
                List<object> chapterNodes = new List<object>();
                string text = tr.ReadToEnd();
                string[] patterns = new string[] {
                "\\b第[\\d\\uFF10-\\uFF19一二三四五六七八九十百千零]+[部章节篇集卷]\\b",
                "\\b[\\d\\uFF10-\\uFF19]+\\b" };
                foreach (string ps in patterns)
                {
                    Regex reg = new Regex(ps);
                    int last = 0;
                    foreach (Match m in Regex.Matches(text, ps))
                    {
                        int i = m.Index;
                        if (!text.Substring(last, i - last).Contains("\n"))
                        {
                            continue;
                        }
                        string id = string.Format("{0}_{1}_{2}", k, last, i);
                        string title = text.Substring(last);
                        title = title.Substring(0, title.IndexOf("\n") - 1);
                        string content = text.Substring(last, i - last);
                        chapterNodes.Add(new { id = id, title = title, content = content });
                        last = i;
                    }
                    if (chapterNodes.Count != 0)
                    {
                        if (last < text.Length)
                        {
                            string id = string.Format("{0}_{1}_{2}", k, last, text.Length);
                            string title = text.Substring(last);
                            title = title.Substring(0, title.IndexOf("\n") - 1);
                            string content = text.Substring(last, text.Length - last);
                            chapterNodes.Add(new { id = id, title = title, content = content });
                        }
                        break;
                    }
                }
                return chapterNodes;
            }
        }
    }
}
