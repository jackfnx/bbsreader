using BBSReader.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace BBSReader
{
    /// <summary>
    /// PacketWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PacketWindow : Window
    {
        private const string REG_EX = "\\b第[\\d\\uFF10-\\uFF19一二三四五六七八九十百千零]+[部章节篇集卷]";
        private ObservableCollection<object> files;
        private ObservableCollection<object> contents;
        private byte[] coverData;

        public PacketWindow()
        {
            InitializeComponent();

            files = new ObservableCollection<object>();
            FileNameListView.DataContext = files;
            RegExListView.DataContext = files;

            contents = new ObservableCollection<object>();
            ContentListView.DataContext = contents;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                List<string> fileDrops = new List<string>((string [])e.Data.GetData(DataFormats.FileDrop));
                fileDrops.RemoveAll(x => !x.EndsWith(".txt") && !x.EndsWith(".zip"));
                fileDrops.RemoveAll(x => x.StartsWith(Path.GetTempPath()));
                if (files.Count != 0)
                {
                    if (fileDrops.Exists(x => x.EndsWith(".zip")))
                    {
                        MessageBox.Show(string.Format("{0} is packet.", fileDrops.Find(x => x.EndsWith(".zip"))));
                        return;
                    }
                    foreach (dynamic ob in files)
                    {
                        string filename = ob.filename;
                        if (fileDrops.Contains(filename))
                        {
                            fileDrops.Remove(filename);
                        }
                    }
                }

                if (files.Count == 0)
                {
                    string zipName = fileDrops.Find(x => x.EndsWith(".zip"));
                    if (zipName != null)
                    {
                        UnzipPacket(zipName);
                        return;
                    }
                    else
                    {
                        string filename = fileDrops[0];
                        string basename = filename.Substring(filename.LastIndexOfAny("/\\".ToCharArray()) + 1);
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
                }
                string regex = REG_EX;
                if (files.Count != 0)
                {
                    dynamic data = files[files.Count - 1];
                    regex = data.regex;
                }
                foreach (string s in fileDrops)
                {
                    string text = Utils.ReadText(s, Encoding.UTF8);
                    foreach (object ch in SearchChapterNodes(text, files.Count, regex))
                    {
                        contents.Add(ch);
                    }
                    files.Add(new { filename = s, text = text, regex = regex });
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            BookTitle.Text = "";
            BookAuthor.Text = "";
            coverData = null;
            CoverUrl.IsEnabled = true;
            CoverUrl.Text = "";
            DownloadCover.Visibility = Visibility.Visible;
            ResetCover.Visibility = Visibility.Collapsed;
            files.Clear();
            contents.Clear();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CoverUrl.Text) && CoverUrl.IsEnabled && !CoverUrl.Text.StartsWith(Path.GetTempPath()))
            {
                if (coverData != null)
                {
                    // do nothing
                }
                else
                {
                    byte[] coverBytes = CoverDownloader.BatchProc(CoverUrl.Text);
                    if (coverBytes == null)
                    {
                        MessageBox.Show("Download Cover Error.");
                        return;
                    }
                    SaveCoverToTempFile(coverBytes);
                }
            }

            Packet packet = new Packet();
            packet.title = BookTitle.Text;
            packet.author = BookAuthor.Text;
            packet.skType = SKType.Manual;
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
            packet.regexps = new List<string>();
            foreach (dynamic ob in files)
            {
                packet.regexps.Add(ob.regex);
            }
            packet.chapters.Reverse();
            packet.timestamp = 0;
            packet.key = Utils.CalcKey(packet.title, packet.author, true);
            packet.summary = Utils.CalcSumary(packet.title, packet.author, true, packet.chapters, coverData);
            packet.source = "TextRepack";

            string folder = string.Format("{0}/packets", Constants.LOCAL_PATH);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string path = string.Format("{0}/{1}_{2}.zip", folder, packet.title, packet.author);

            PacketToZip(packet, path, contents, coverData);
            MessageBox.Show(string.Format("{0} saved.", path));
        }

        private void RegExpEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            dynamic fn = button.DataContext;

            RegExEditor editor = new RegExEditor();
            editor.text = fn.text;
            editor.RegExp.Text = fn.regex;
            if (editor.ShowDialog() ?? true)
            {
                string regex = editor.RegExp.Text;
                int k = files.IndexOf(fn);
                files.RemoveAt(k);
                files.Insert(0, new { filename = fn.filename, text = fn.text, regex = regex });
                string prefix = string.Format("{0}_", k);
                int j = contents.Count;
                for (int i = contents.Count - 1; i >= 0; i--)
                {
                    dynamic ch = contents[i];
                    if (ch.id.StartsWith(prefix))
                    {
                        contents.Remove(ch);
                        j = i;
                    }
                }
                List<object> newChapters = SearchChapterNodes(fn.text, k, regex);
                for (int i = newChapters.Count - 1; i >= 0; i--)
                {
                    dynamic ch = newChapters[i];
                    contents.Insert(j, ch);
                }
            }
        }

        private static void PacketToZip(Packet packet, string path, ObservableCollection<object> contents, byte[] coverData)
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
                    if (coverData != null)
                    {
                        ZipArchiveEntry cover = zip.CreateEntry("cover.jpg");
                        using (BinaryWriter sw = new BinaryWriter(cover.Open()))
                        {
                            sw.Write(coverData);
                        }
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

        private void UnzipPacket(string zipName)
        {
            using (ZipArchive zip = ZipFile.OpenRead(zipName))
            {
                Packet packet;
                ZipArchiveEntry meta = zip.GetEntry(".META.json");
                using (StreamReader sr = new StreamReader(meta.Open()))
                {
                    string metaJson = sr.ReadToEnd();
                    packet = JsonConvert.DeserializeObject<Packet>(metaJson);
                }
                BookTitle.Text = packet.title;
                BookAuthor.Text = packet.author;
                ZipArchiveEntry content = zip.GetEntry(".CONTENT");
                using (StreamReader sr = new StreamReader(content.Open()))
                {
                    string contentJson = sr.ReadToEnd();
                    packet.chapters = JsonConvert.DeserializeObject<List<Chapter>>(contentJson);
                }
                ZipArchiveEntry cover = zip.GetEntry("cover.jpg");
                if (cover != null)
                {
                    using (BinaryReader sr = new BinaryReader(cover.Open()))
                    {
                        SaveCoverToTempFile(sr.ReadBytes((int)cover.Length));
                    }
                }
                if (packet.source == "TextRepack")
                {
                    string currentId = "";
                    string currentText = "";
                    Action saveCurrentText = delegate
                    {
                        if (!string.IsNullOrEmpty(currentText))
                        {
                            string nextPath = Utils.GenerateTempFileName(".txt");
                            using (FileStream fs = new FileStream(nextPath, FileMode.Create))
                            using (StreamWriter sw = new StreamWriter(fs))
                            {
                                sw.Write(currentText);
                            }
                            string regex = REG_EX;
                            if (packet.regexps != null && packet.regexps.Count > files.Count)
                            {
                                regex = packet.regexps[files.Count];
                            }
                            files.Add(new { filename = nextPath, text = currentText, regex = regex });
                        }
                    };
                    for (int i = 0; i < packet.chapters.Count; i++)
                    {
                        Chapter ch = packet.chapters[i];
                        ZipArchiveEntry che = zip.GetEntry(ch.savePath);
                        using (StreamReader sr = new StreamReader(che.Open()))
                        {
                            string text = sr.ReadToEnd();
                            string textId = ch.id.Substring(0, ch.id.IndexOf("_"));
                            if (textId == currentId)
                            {
                                currentText = text + currentText;
                            }
                            else
                            {
                                saveCurrentText();
                                currentText = text;
                                currentId = textId;
                            }
                            contents.Insert(0, new { id = ch.id, title = ch.title, content = text });
                        }
                    }
                    saveCurrentText();
                }
                else
                {
                    MessageBox.Show("It is not a 'TextRepack' packet.");
                }
            }
        }

        private void UpdateChapterNodes(string text, int k, string pattern)
        {
            string prefix = string.Format("{0}_", k);
            List<object> newChapters = SearchChapterNodes(text, k, pattern);
            int s = -1;
            int e = contents.Count;
            for (int i = 0; i < contents.Count; i++)
            {
                dynamic ch = contents[i];
                string id = ch.id;
                if (s == 1 && id.StartsWith(prefix))
                {
                    s = i;
                }
                if (s != -1 && !id.StartsWith(prefix))
                {
                    e = i;
                    break;
                }
            }
            if (s == -1)
            {
                s = contents.Count;
            }
            for (int i = s; i < e; i++)
            {
                contents.RemoveAt(s);
            }
            for (int i = newChapters.Count; i >= 0; i--)
            {
                contents.Insert(s, newChapters[i]);
            }
        }

        private static List<object> SearchChapterNodes(string text, int k, string pattern)
        {
            Func<int, int, int, object> generateChapter = delegate(int key, int from, int to)
            {
                string id = string.Format("{0}_{1}_{2}", key, from, to);
                string title = text.Substring(from);
                title = title.Substring(0, title.IndexOf("\n") - 1);
                string content = text.Substring(from, to - from);
                return new { id = id, title = title, content = content };
            };
            List<object> chapterNodes = new List<object>();
            int last = 0;
            foreach (Match m in Regex.Matches(text, pattern))
            {
                int i = m.Index;
                if (!text.Substring(last, i - last).Contains("\n"))
                {
                    continue;
                }
                chapterNodes.Add(generateChapter(k, last, i));
                last = i;
            }
            if (chapterNodes.Count != 0)
            {
                if (last < text.Length)
                {
                    chapterNodes.Add(generateChapter(k, last, text.Length));
                }
            }
            return chapterNodes;
        }

        private void DownloadCover_Click(object sender, RoutedEventArgs e)
        {
            CoverDownloader downloader = new CoverDownloader();
            downloader.CoverUrl.Text = CoverUrl.Text;
            if (downloader.ShowDialog() ?? true)
            {
                SaveCoverToTempFile(downloader.coverData);
            }
        }

        private void ResetCover_Click(object sender, RoutedEventArgs e)
        {
            coverData = null;
            CoverUrl.IsEnabled = true;
            CoverUrl.Text = "";
            DownloadCover.Visibility = Visibility.Visible;
            ResetCover.Visibility = Visibility.Collapsed;
        }

        private void SaveCoverToTempFile(byte[] coverBytes)
        {
            string coverPath = Utils.GenerateTempFileName(".jpg");
            using (FileStream fs = new FileStream(coverPath, FileMode.Create))
            using (BinaryWriter sw = new BinaryWriter(fs))
            {
                sw.Write(coverBytes);
            }
            coverData = coverBytes;
            CoverUrl.IsEnabled = false;
            CoverUrl.Text = coverPath;
            DownloadCover.Visibility = Visibility.Collapsed;
            ResetCover.Visibility = Visibility.Visible;
        }
    }
}
