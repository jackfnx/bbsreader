﻿using BBSReader.Data;
using BBSReader.PacketServer;
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
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace BBSReader
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //private ReaderWindow readerWindow;
        public ObservableCollection<object> topics;
        public ObservableCollection<object> articles;
        private MetaData metaData;
        private int currentId;
        private int readId;
        private string searchingKeyword;
        private string text;
        private AppState currentState;
        private Notifier notifier;

        public enum AppState
        {
            TOPICS,
            ARTICLES,
            READER
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
            
            this.metaData = MetaDataLoader.Load();

            topics = new ObservableCollection<object>();
            TopicListView.DataContext = topics;

            articles = new ObservableCollection<object>();
            ArticleListView.DataContext = articles;

            ServerInd.DataContext = MyServer.GetInstance();

            searchingKeyword = null;
            ReloadTopics();

            currentState = AppState.TOPICS;
            currentId = -1;

            text = "";
            UpdateView();
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
                readId = -1;
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
                var item = new ListItem
                {
                    Title = x
                };

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

                BBSThread example = x.groupedTids.Count > 0 ?  metaData.threads[x.groupedTids[0].exampleId] : new BBSThread { postTime = "1970-01-01", threadId = "0" };
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
                item.Tooltip = null;

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
                list = metaData.tags[title].ConvertAll(x => new Group { exampleId = x, tooltips = "Not Download." });
                read = list.Count;
            }

            articles.Clear();
            list.ForEach(x =>
            {
                int i = list.Count - list.IndexOf(x) - 1;
                BBSThread t = metaData.threads[x.exampleId];
                if (searchingKeyword == null || t.title.Contains(searchingKeyword))
                {
                    string siteName = Constants.SITE_DEF[t.siteId].siteName;
                    string fPath = Constants.LOCAL_PATH + t.siteId + "/" + t.threadId + ".txt";
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
            {
                searchingKeyword = null;
                ReloadTopics();
            }
            UpdateView();
        }

        private void ForwardAtArticles(dynamic item)
        {
            string fPath = Constants.LOCAL_PATH + item.SiteId + "/" + item.ThreadId + ".txt";
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
                    MetaDataLoader.Save(this.metaData);
                    ReloadArticles();
                    ReloadTopics();
                }
                readId = index;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (SearchBox.IsFocused)
            {
                return;
            }
            
            if (e.Key == System.Windows.Input.Key.Space)
            {
                if (currentState == AppState.READER)
                {
                    ReaderScroll.PageDown();
                    //ReaderScroll.LineUp();
                    //ReaderScroll.LineUp();
                    ReaderScroll.Focus();
                }
                else if (currentState == AppState.ARTICLES)
                {
                    int i = ArticleListView.SelectedIndex;
                    if (i < 0)
                    {
                        ArticleListView.SelectedIndex = 0;
                    }
                    if (ArticleListView.ItemContainerGenerator.ContainerFromIndex(ArticleListView.SelectedIndex) is ListViewItem item)
                        item.Focus();
                }
                else if (currentState == AppState.TOPICS)
                {
                    int i = TopicListView.SelectedIndex;
                    if (i < 0)
                    {
                        TopicListView.SelectedIndex = 0;
                    }
                    if (TopicListView.ItemContainerGenerator.ContainerFromIndex(TopicListView.SelectedIndex) is ListViewItem item)
                        item.Focus();
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
                    {
                        ForwardAtTopics(item);
                    }
                }
                else if (currentState == AppState.ARTICLES)
                {
                    dynamic item = ArticleListView.SelectedItem;
                    if (item == null)
                        item = ArticleListView.SelectedItem;
                    if (item != null)
                    {
                        ForwardAtArticles(item);
                    }
                }
            }
            else if (e.Key == Key.Left || e.Key == Key.E || e.Key == Key.Q)
            {
                Backward();
            }
            else if (e.Key == Key.J)
            {
                if (currentState == AppState.READER)
                {
                    if (readId > 0)
                    {
                        dynamic nextItem = articles[readId - 1];
                        ForwardAtArticles(nextItem);
                    }
                    else
                    {
                        notifier.ShowInformation("There is the LAST chapter.");
                    }
                }
            }
            else if (e.Key == Key.K)
            {
                if (currentState == AppState.READER)
                {
                    if (readId + 1 < articles.Count())
                    {
                        dynamic prevItem = articles[readId + 1];
                        ForwardAtArticles(prevItem);
                    }
                    else
                    {
                        notifier.ShowInformation("There is the FIRST chapter.");
                    }
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MyServer.GetInstance().Start();

            ResetFont();

            notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new WindowPositionProvider(
                    parentWindow: Application.Current.MainWindow,
                    corner: Corner.BottomCenter,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });
        }
        
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            notifier.Dispose();
        }
        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MyServer.GetInstance().Stop();
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
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);
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
                SuperKeyword superKeyword = new SuperKeyword
                {
                    simple = true,
                    keyword = title,
                    authors = new List<string> { item.Author },
                    tids = tids,
                    read = -1,
                    groups = new List<List<string>>(),
                    groupedTids = tids.ConvertAll(x => new Group { exampleId = x, tooltips = "" })
                };
                metaData.superKeywords.Add(superKeyword);
                MetaDataLoader.Save(this.metaData);
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
                MetaDataLoader.Save(this.metaData);
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
                MetaDataLoader.Save(this.metaData);
                currentId = -1;
                currentState = AppState.TOPICS;
                ReloadTopics();
                UpdateView();
            }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            ScriptDialog dialog = new ScriptDialog(ScriptDialog.ScriptId.UPDATE_ALL);
            if (dialog.ShowDialog() ?? false)
            {
                this.metaData = MetaDataLoader.Load();
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

            var dialog = new AdvancedKeywordDialog
            {
                Owner = this,
                Keyword = title,
                DefaultAuthor = author
            };
            var dr = dialog.ShowDialog() ?? false;
            if (dr)
            {
                SuperKeyword superKeyword = new SuperKeyword
                {
                    simple = false,
                    keyword = dialog.Keyword,
                    authors = new List<string>(dialog.AuthorList),
                    tids = new List<int>(),
                    read = -1,
                    groups = new List<List<string>>(),
                    groupedTids = new List<Group>()
                };
                if (!metaData.superKeywords.Contains(superKeyword))
                {
                    metaData.superKeywords.Add(superKeyword);
                    MetaDataLoader.Save(this.metaData);
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
                MetaDataLoader.Save(this.metaData);
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

            Process.Start(Constants.SITE_DEF[siteId].siteHost + link);
        }

        private void DeleteTxtContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string siteId = item.SiteId;
            string threadId = item.ThreadId;

            string fPath = Constants.LOCAL_PATH + item.SiteId + "/" + item.ThreadId + ".txt";
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
                        MetaDataLoader.Save(this.metaData);
                        ReloadArticles();
                        ReloadTopics();
                        UpdateView();
                    }
                }
            }
        }

        private void DownloadDetailContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            int siteId = Constants.SITE_DEF[item.SiteId].id;
            long threadId = long.Parse(item.ThreadId);

            ScriptDialog dialog = new ScriptDialog(ScriptDialog.ScriptId.DOWNLOAD_ONE_DETAIL, threadId, siteId);
            if (dialog.ShowDialog() ?? false)
            {
                ReloadArticles();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
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

        private void PacketButton_Click(object sender, RoutedEventArgs e)
        {
            PacketWindow dialog = new PacketWindow();
            dialog.ShowDialog();
        }
    }
}
