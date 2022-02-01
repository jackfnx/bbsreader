using BBSReader.Data;
using BBSReader.PacketServer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        public ObservableCollection<object> corpus;
        public ObservableCollection<object> articles;
        private MetaData metaData;
        private StateStack currPath;
        private bool isLoading;
        private string text;
        private Notifier notifier;

        private class StateStack
        {
            internal enum State
            {
                TOPICS,
                CORPUS,
                ARTICLES,
                SEARCHING,
                READER,
                LOADING
            }

            internal class StateDesc
            {
                public State state;
                public int itemId;
                public string searchingKeyword;
            }

            internal List<StateDesc> stack;

            internal StateStack()
            {
                stack = new List<StateDesc>();
                Push(State.TOPICS);
            }

            internal void Push(State state, int itemId = -1, string searchingKeyword = null)
            {
                stack.Add(new StateDesc() { state = state, itemId = itemId, searchingKeyword = searchingKeyword });
            }

            internal StateDesc Pop()
            {
                StateDesc state = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
                return state;
            }

            internal void Clear()
            {
                stack.RemoveRange(1, stack.Count - 1);
            }

            internal StateDesc GetCurrState()
            {
                StateDesc state = stack[stack.Count - 1];
                return state;
            }

            internal StateDesc GetPrevState(StateDesc currState)
            {
                int i = stack.IndexOf(currState);
                StateDesc state = stack[i - 1];
                return state;
            }

            internal StateDesc FindLastState(State state)
            {
                for (int i = stack.Count - 1; i >= 0; i--)
                {
                    if (stack[i].state == state)
                    {
                        return stack[i];
                    }
                }
                return null;
            }

            internal void UpdateCurrState(int itemId = -1, string searchingKeyword = null)
            {
                if (itemId >= 0)
                    stack[stack.Count - 1].itemId = itemId;
                if (searchingKeyword != null)
                    stack[stack.Count - 1].searchingKeyword = searchingKeyword;
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
            internal bool ManualDisabled;
            internal string SKType;
            internal int FavoriteId;
            internal bool IsFolder;
            internal bool Downloaded;
            internal bool Read;
            internal string Tooltip;
        }

        public MainWindow()
        {
            InitializeComponent();

            topics = new ObservableCollection<object>();
            TopicListView.DataContext = topics;

            corpus = new ObservableCollection<object>();
            CorpusListView.DataContext = corpus;

            articles = new ObservableCollection<object>();
            ArticleListView.DataContext = articles;

            ServerInd.DataContext = MyServer.GetInstance();

            currPath = new StateStack();

            text = "";

            StartReloadData();
        }

        private void StartReloadData()
        {
            Thread thread = new Thread(ReloadData);
            thread.Start();
        }

        private void ReloadData()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                isLoading = true;
                UpdateView();
            }));
            this.metaData = MetaDataLoader.Load();
            var topicsItems = ReloadTopics();
            this.Dispatcher.Invoke(new Action(() =>
            {
                UpdateTopics(topicsItems);
                isLoading = false;
                currPath.Clear();
                UpdateView();
            }));
        }

        private void StartResetUI()
        {
            Thread thread = new Thread(ResetUI);
            thread.Start();
        }

        private void ResetUI()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                isLoading = true;
                UpdateView();
            }));
            var topicsItems = ReloadTopics();
            this.Dispatcher.Invoke(new Action(() =>
            {
                UpdateTopics(topicsItems);
                isLoading = false;
                currPath.Clear();
                UpdateView();
            }));
        }

        private void UpdateView()
        {
            if (isLoading)
            {
                TopicListView.Visibility = Visibility.Hidden;
                CorpusListView.Visibility = Visibility.Hidden;
                ArticleListView.Visibility = Visibility.Hidden;
                ReaderView.Visibility = Visibility.Hidden;
                LoadingIcon.Visibility = Visibility.Visible;
                BackButton.IsEnabled = false;
            }
            else
            {
                var currState = currPath.GetCurrState();
                if (currState.state == StateStack.State.TOPICS)
                {
                    TopicListView.Visibility = Visibility.Visible;
                    CorpusListView.Visibility = Visibility.Hidden;
                    ArticleListView.Visibility = Visibility.Hidden;
                    ReaderView.Visibility = Visibility.Hidden;
                    LoadingIcon.Visibility = Visibility.Hidden;
                }
                else if (currState.state == StateStack.State.CORPUS)
                {
                    TopicListView.Visibility = Visibility.Hidden;
                    CorpusListView.Visibility = Visibility.Visible;
                    ArticleListView.Visibility = Visibility.Hidden;
                    ReaderView.Visibility = Visibility.Hidden;
                    LoadingIcon.Visibility = Visibility.Hidden;
                }
                else if (currState.state == StateStack.State.ARTICLES)
                {
                    TopicListView.Visibility = Visibility.Hidden;
                    CorpusListView.Visibility = Visibility.Hidden;
                    ArticleListView.Visibility = Visibility.Visible;
                    ReaderView.Visibility = Visibility.Hidden;
                    LoadingIcon.Visibility = Visibility.Hidden;
                }
                else if (currState.state == StateStack.State.READER)
                {
                    TopicListView.Visibility = Visibility.Hidden;
                    CorpusListView.Visibility = Visibility.Hidden;
                    ArticleListView.Visibility = Visibility.Hidden;
                    ReaderView.Visibility = Visibility.Visible;
                    LoadingIcon.Visibility = Visibility.Hidden;
                }

                BackButton.IsEnabled = currPath.stack.Count > 1;
            }
        }

        private List<ListItem> ReloadTopics()
        {
            List<string> tags = new List<string>(metaData.tags.Keys);
            var superkeywords = new List<SuperKeyword>(metaData.superKeywords);

            var currState = currPath.GetCurrState();
            if (currState.searchingKeyword != null)
            {
                tags.RemoveAll(x =>
                {
                    if (x.Contains(currState.searchingKeyword))
                        return false;
                    BBSThread exampleX = metaData.threads[metaData.tags[x][0]];
                    return !exampleX.author.Contains(currState.searchingKeyword);
                });
                superkeywords.RemoveAll(x =>
                {
                    if (x.keyword.Contains(currState.searchingKeyword))
                        return false;
                    foreach (string author in x.authors)
                    {
                        if (author.Contains(currState.searchingKeyword))
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
                item.ManualDisabled = false;
                item.SKType = SKType.Simple;
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

                if (x.skType == SKType.Simple)
                    item.Title = x.keyword;
                else if (x.skType == SKType.Manual)
                {
                    if (keyword == "*")
                        item.Title = ("《独立篇章合集》");
                    else
                        item.Title = ("静态：【" + keyword + "】");
                }
                else if (x.skType == SKType.Author)
                    item.Title = ("【" + author + "】的作品集");
                else if (x.skType == SKType.Advanced)
                {
                    if (author == "*")
                        item.Title = ("专题：【" + keyword + "】");
                    else
                        item.Title = ("【" + keyword + "】系列");
                }
                item.Author = author;

                BBSThread example = x.groupedTids.Count > 0 ? metaData.threads[x.groupedTids[0].exampleId] : new BBSThread { postTime = "1970-01-01", threadId = "0" };
                item.Time = example.postTime;
                item.Url = example.link;
                item.Source = "";
                item.SiteId = example.siteId;
                item.ThreadId = example.threadId;
                item.Favorite = true;
                item.ManualDisabled = false;
                item.SKType = x.skType;
                item.FavoriteId = metaData.superKeywords.IndexOf(x);
                item.IsFolder = true;
                item.Downloaded = false;
                if (x.skType == SKType.Author)
                {
                    item.Read = x.subSKs.All(y => y.groupedTids.Count <= y.read + 1) && (x.noSKGTids.Count <= x.read + 1);
                }
                else
                {
                    item.Read = x.groupedTids.Count <= x.read + 1;
                }
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

            return items;
        }

        private void UpdateTopics(List<ListItem> items)
        {
            topics.Clear();
            items.ForEach(x =>
            {
                topics.Add(new { x.Title, x.Author, x.Time, x.Url, x.ThreadId, x.Source, x.SiteId, x.Favorite, x.ManualDisabled, x.SKType, x.FavoriteId, x.IsFolder, x.Downloaded, x.Read, x.Tooltip });
            });
        }

        private void ReloadCorpus()
        {
            var corpusState = currPath.FindLastState(StateStack.State.CORPUS);
            if (corpusState == null)
            {
                return;
            }
            dynamic item = topics[corpusState.itemId];
            SuperKeyword sk = metaData.superKeywords[item.FavoriteId];

            List<ListItem> items = new List<ListItem>();
            var list1 = sk.subSKs;
            list1.ForEach(x =>
            {
                int i = list1.IndexOf(x);

                string author = x.authors[0];
                string keyword = x.keyword;

                BBSThread example = x.groupedTids.Count > 0 ? metaData.threads[x.groupedTids[0].exampleId] : new BBSThread { postTime = "1970-01-01", threadId = "0" };
                string tooltip = null;

                items.Add(new ListItem()
                {
                    Title = keyword,
                    Author = author,
                    Time = example.postTime,
                    Url = example.link,
                    ThreadId = example.threadId,
                    Source = "",
                    SiteId = example.siteId,
                    Favorite = true,
                    ManualDisabled = true,
                    SKType = SKType.Simple,
                    FavoriteId = i,
                    IsFolder = true,
                    Downloaded = false,
                    Read = x.groupedTids.Count <= x.read + 1,
                    Tooltip = tooltip
                }) ;
            });
            List<Group> list2 = sk.noSKGTids;
            list2.ForEach(x =>
            {
                int i = list2.Count - list2.IndexOf(x) - 1;
                BBSThread t = metaData.threads[x.exampleId];
                string siteName = Constants.SITE_DEF[t.siteId].siteName;
                string fPath = Constants.LOCAL_PATH + t.siteId + "/" + t.threadId + ".txt";
                bool downloaded = File.Exists(fPath);
                items.Add(new ListItem()
                {
                    Title = t.title,
                    Author = t.author,
                    Time = t.postTime,
                    Url = t.link,
                    ThreadId = t.threadId,
                    Source = siteName,
                    SiteId = t.siteId,
                    Favorite = false,
                    ManualDisabled = false,
                    SKType = SKType.Simple,
                    FavoriteId = -1,
                    IsFolder = false,
                    Downloaded = downloaded,
                    Read = i <= sk.read,
                    Tooltip = x.tooltips
                });
            });
            items.Sort((x, y) =>
            {
                if (x.IsFolder && !y.IsFolder)
                {
                    return -1;
                }
                else if (!x.IsFolder && y.IsFolder)
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

            corpus.Clear();
            items.ForEach(x =>
            {
                corpus.Add(new { x.Title, x.Author, x.Time, x.Url, x.ThreadId, x.Source, x.SiteId, x.Favorite, x.ManualDisabled, x.SKType, x.FavoriteId, x.IsFolder, x.Downloaded, x.Read, x.Tooltip });
            });
        }

        private void ReloadArticles()
        {
            var articlesState = currPath.FindLastState(StateStack.State.ARTICLES);
            if (articlesState == null)
            {
                return;
            }
            var prevState = currPath.GetPrevState(articlesState);
            dynamic item;
            List<Group> list;
            int read;
            if (prevState.state == StateStack.State.CORPUS)
            {
                item = corpus[articlesState.itemId];
                dynamic topicItem = topics[prevState.itemId];
                SuperKeyword sk = metaData.superKeywords[topicItem.FavoriteId];
                SuperKeyword subSK = sk.subSKs[item.FavoriteId];
                list = subSK.groupedTids;
                read = subSK.read;
            }
            else
            {
                item = topics[articlesState.itemId];
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
            }

            articles.Clear();
            list.ForEach(x =>
            {
                int i = list.Count - list.IndexOf(x) - 1;
                BBSThread t = metaData.threads[x.exampleId];
                if (articlesState.searchingKeyword == null || t.title.Contains(articlesState.searchingKeyword))
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
                        ManualDisabled = false,
                        SKType = SKType.Simple,
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
            var currState = currPath.GetCurrState();
            if (currState.state == StateStack.State.TOPICS)
            {
                ForwardAtTopics(item);
            }
            else if (currState.state == StateStack.State.CORPUS)
            {
                ForwardAtCorpus(item);
            }
            else if (currState.state == StateStack.State.ARTICLES)
            {
                ForwardAtArticles(item);
            }
        }

        private void ForwardAtTopics(dynamic item)
        {
            int topicId = topics.IndexOf(item);
            int FavoriteId = item.FavoriteId;
            var currState = currPath.GetCurrState();
            if (FavoriteId >= 0 && (metaData.superKeywords[FavoriteId].skType == SKType.Author))
            {
                currPath.Push(StateStack.State.CORPUS, topicId, currState.searchingKeyword);
                ReloadCorpus();
            }
            else
            {
                currPath.Push(StateStack.State.ARTICLES, topicId, currState.searchingKeyword);
                ReloadArticles();
            }
            UpdateView();
        }

        private void ForwardAtCorpus(dynamic item)
        {
            int corpusId = corpus.IndexOf(item);
            var currState = currPath.GetCurrState();
            if (item.IsFolder)
            {
                currPath.Push(StateStack.State.ARTICLES, corpusId, currState.searchingKeyword);
                ReloadArticles();
            }
            else
            {
                ForwardAtArticles(item);
            }
            UpdateView();
        }

        private void ForwardAtArticles(dynamic item)
        {
            string fPath = Constants.LOCAL_PATH + item.SiteId + "/" + item.ThreadId + ".txt";
            if (File.Exists(fPath))
            {
                var currState = currPath.GetCurrState();
                var prevState = currPath.GetPrevState(currState);

                using (StreamReader sr = new StreamReader(fPath, new UTF8Encoding(false)))
                {
                    text = sr.ReadToEnd();
                    currPath.Push(StateStack.State.READER);
                    ReaderText.Text = text;
                    ReaderScroll.ScrollToHome();
                    UpdateView();
                }

                if (prevState.state == StateStack.State.TOPICS)
                {
                    dynamic topic = topics[currState.itemId];
                    int FavoriteId = topic.FavoriteId;
                    var sk = metaData.superKeywords[FavoriteId];
                    int index;
                    int i;
                    if (currState.state != StateStack.State.CORPUS)
                    {
                        index = articles.IndexOf(item);
                        i = sk.groupedTids.Count - index - 1;
                    }
                    else
                    {
                        index = corpus.IndexOf(item);
                        i = sk.subSKs.Count + sk.noSKGTids.Count - index - 1;
                    }
                    currPath.UpdateCurrState(index);
                    if (i > sk.read)
                    {
                        sk.read = i;
                        metaData.superKeywords[FavoriteId] = sk;
                        MetaDataLoader.Save(this.metaData);

                        if (currState.state != StateStack.State.CORPUS)
                        {
                            ReloadArticles();
                        }
                        else
                        {
                            ReloadCorpus();
                        }
                        UpdateTopics(ReloadTopics());
                    }
                }
                else if (prevState.state == StateStack.State.CORPUS)
                {
                    dynamic topic = topics[prevState.itemId];
                    int FavoriteId = topic.FavoriteId;
                    dynamic corpusItem = corpus[currState.itemId];
                    int subFavId = corpusItem.FavoriteId;
                    int index = articles.IndexOf(item);
                    var sk = metaData.superKeywords[FavoriteId];
                    var subSK = sk.subSKs[subFavId];
                    int i = subSK.groupedTids.Count - index - 1;
                    currPath.UpdateCurrState(index);
                    if (i > subSK.read)
                    {
                        subSK.read = i;
                        sk.subReads[subFavId] = i;
                        sk.subSKs[subFavId] = subSK;
                        metaData.superKeywords[FavoriteId] = sk;
                        MetaDataLoader.Save(this.metaData);
                        ReloadArticles();
                        ReloadCorpus();
                        UpdateTopics(ReloadTopics());
                    }
                }
            }
        }

        private void Backward()
        {
            var currState = currPath.GetCurrState();
            var prevState = currPath.GetPrevState(currState);
            currPath.Pop();
            if (currState.searchingKeyword != null && prevState.searchingKeyword == null)
            {
                UpdateTopics(ReloadTopics());
            }
            UpdateView();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (SearchBox.IsFocused)
            {
                if (e.Key == Key.Enter)
                {
                    DoSearch();
                }
                return;
            }

            var currState = currPath.GetCurrState();
            if (e.Key == Key.Space)
            {
                if (currState.state == StateStack.State.READER)
                {
                    ReaderScroll.PageDown();
                    //ReaderScroll.LineUp();
                    //ReaderScroll.LineUp();
                    ReaderScroll.Focus();
                }
                else if (currState.state == StateStack.State.ARTICLES)
                {
                    int i = ArticleListView.SelectedIndex;
                    if (i < 0)
                    {
                        ArticleListView.SelectedIndex = 0;
                    }
                    if (ArticleListView.ItemContainerGenerator.ContainerFromIndex(ArticleListView.SelectedIndex) is ListViewItem item)
                        item.Focus();
                }
                else if (currState.state == StateStack.State.CORPUS)
                {
                    int i = CorpusListView.SelectedIndex;
                    if (i < 0)
                    {
                        CorpusListView.SelectedIndex = 0;
                    }
                    if (CorpusListView.ItemContainerGenerator.ContainerFromIndex(CorpusListView.SelectedIndex) is ListViewItem item)
                        item.Focus();
                }
                else if (currState.state == StateStack.State.TOPICS)
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
                if (currState.state == StateStack.State.TOPICS)
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
                else if (currState.state == StateStack.State.CORPUS)
                {
                    dynamic item = CorpusListView.SelectedIndex;
                    if (item == null)
                        CorpusListView.SelectedIndex = 0;
                    item = CorpusListView.SelectedItem;
                    if (item != null)
                    {
                        ForwardAtCorpus(item);
                    }
                }
                else if (currState.state == StateStack.State.ARTICLES)
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
                if (currState.state == StateStack.State.READER)
                {
                    if (currState.itemId > 0)
                    {
                        dynamic nextItem = articles[currState.itemId - 1];
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
                if (currState.state == StateStack.State.READER)
                {
                    if (currState.itemId + 1 < articles.Count())
                    {
                        dynamic prevItem = articles[currState.itemId + 1];
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
                    skType = SKType.Simple,
                    keyword = title,
                    authors = new List<string> { item.Author },
                    alias = new List<string>(),
                    tids = tids,
                    kws = new List<List<int>>(),
                    read = -1,
                    subReads = new List<int>(),
                    groups = new List<List<string>>(),
                    groupedTids = tids.ConvertAll(x => new Group { exampleId = x, tooltips = "" })
                };
                metaData.superKeywords.Add(superKeyword);
                MetaDataLoader.Save(this.metaData);
                StartResetUI();
            }
        }

        private void RemoveFavoritesContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string title = item.Title;
            int FavoriteId = item.FavoriteId;
            SuperKeyword superKeyword = metaData.superKeywords[FavoriteId];
            if (superKeyword.skType == SKType.Simple)
            {
                var tids = superKeyword.tids;
                var keyword = superKeyword.keyword;
                metaData.superKeywords.Remove(superKeyword);
                metaData.tags[keyword] = tids;
                MetaDataLoader.Save(this.metaData);
                StartResetUI();
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
                StartResetUI();
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            ScriptDialog dialog = new ScriptDialog(ScriptDialog.ScriptId.UPDATE_ALL) { Owner = this };
            if (dialog.ShowDialog() ?? false)
            {
                StartReloadData();
            }
        }

        private void SetAdvancedKeywordContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            int FavoriteId;
            string title;
            List<string> authors = new List<string>();
            if (item.SKType == SKType.Simple)
            {
                FavoriteId = -1;
                title = item.Title;
                authors.Add(item.Author);
            }
            else if (item.SKType == SKType.Advanced)
            {
                FavoriteId = item.FavoriteId;
                SuperKeyword sk = metaData.superKeywords[FavoriteId];
                title = sk.keyword;
                authors = sk.authors;
            }
            else if (item.SKType == SKType.Manual)
            {
                return;
            }
            else
            {
                return;
            }
            var dialog = new AdvancedKeywordDialog
            {
                Owner = this,
                Keyword = title
            };
            authors.ForEach(x => dialog.AuthorList.Add(x));
            var dr = dialog.ShowDialog() ?? false;
            if (dr)
            {
                if (FavoriteId != -1)
                {
                    SuperKeyword superKeyword = metaData.superKeywords[FavoriteId];
                    superKeyword.skType = SKType.Advanced;
                    superKeyword.keyword = dialog.Keyword;
                    superKeyword.authors = new List<string>(dialog.AuthorList);
                    superKeyword.alias = new List<string>();
                    superKeyword.tids = new List<int>();
                    superKeyword.kws = new List<List<int>>();
                    superKeyword.read = -1;
                    superKeyword.groups = new List<List<string>>();
                    superKeyword.groupedTids = new List<Group>();
                    metaData.superKeywords[FavoriteId] = superKeyword;
                }
                else
                {
                    SuperKeyword superKeyword = new SuperKeyword
                    {
                        skType = SKType.Advanced,
                        keyword = dialog.Keyword,
                        authors = new List<string>(dialog.AuthorList),
                        alias = new List<string>(),
                        tids = new List<int>(),
                        kws = new List<List<int>>(),
                        read = -1,
                        subReads = new List<int>(),
                        groups = new List<List<string>>(),
                        groupedTids = new List<Group>()
                    };
                    metaData.superKeywords.Add(superKeyword);
                }
                MetaDataLoader.Save(this.metaData);
                StartResetUI();
            }
        }

        private void CancelAdvancedKeywordContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            if (item.SKType == SKType.Advanced || item.SKType == SKType.Author)
            {
                int FavoriteId = item.FavoriteId;
                metaData.superKeywords.RemoveAt(FavoriteId);
                MetaDataLoader.Save(this.metaData);
                StartResetUI();
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
            var currState = currPath.GetCurrState();
            if (currState.state == StateStack.State.ARTICLES)
                ReloadArticles();
            else if (currState.state == StateStack.State.CORPUS)
                ReloadCorpus();
        }

        private void UnreadContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            if (!item.IsFolder)
            {
                var currState = currPath.GetCurrState();
                var prevState = currPath.GetPrevState(currState);

                if (prevState.state == StateStack.State.TOPICS)
                {
                    dynamic topic = topics[currState.itemId];
                    if (topic.Favorite)
                    {
                        int FavoriteId = topic.FavoriteId;
                        var sk = metaData.superKeywords[FavoriteId];
                        int index;
                        int i;
                        if (currState.state != StateStack.State.CORPUS)
                        {
                            index = articles.IndexOf(item);
                            i = sk.groupedTids.Count - index - 1;
                        }
                        else
                        {
                            index = corpus.IndexOf(item);
                            i = sk.subSKs.Count + sk.noSKGTids.Count - index - 1;
                        }
                        if ((i - 1) <= sk.read)
                        {
                            sk.read = i - 1;
                            metaData.superKeywords[FavoriteId] = sk;
                            MetaDataLoader.Save(this.metaData);
                            if (currState.state != StateStack.State.CORPUS)
                            {
                                ReloadArticles();
                            }
                            else
                            {
                                ReloadCorpus();
                            }
                            UpdateTopics(ReloadTopics());
                            UpdateView();
                        }
                    }
                }
                else if (prevState.state == StateStack.State.CORPUS)
                {
                    dynamic topic = topics[prevState.itemId];
                    if (topic.Favorite)
                    {
                        int FavoriteId = topic.FavoriteId;
                        dynamic corpusItem = corpus[currState.itemId];
                        int subFavId = corpusItem.FavoriteId;
                        int index = articles.IndexOf(item);
                        var sk = metaData.superKeywords[FavoriteId];
                        var subSK = sk.subSKs[subFavId];
                        int i = subSK.groupedTids.Count - index - 1;
                        if ((i - 1) <= subSK.read)
                        {
                            subSK.read = i - 1;
                            sk.subReads[subFavId] = i - 1;
                            sk.subSKs[subFavId] = subSK;
                            metaData.superKeywords[FavoriteId] = sk;
                            MetaDataLoader.Save(this.metaData);
                            ReloadArticles();
                            ReloadCorpus();
                            UpdateTopics(ReloadTopics());
                            UpdateView();
                        }
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

            ScriptDialog dialog = new ScriptDialog(ScriptDialog.ScriptId.DOWNLOAD_ONE_DETAIL, siteId, threadId, "-u", "") { Owner = this };
            if (dialog.ShowDialog() ?? false)
            {
                var currState = currPath.GetCurrState();
                if (currState.state == StateStack.State.ARTICLES)
                    ReloadArticles();
                else if (currState.state == StateStack.State.CORPUS)
                    ReloadCorpus();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            DoSearch();
        }

        private void DoSearch()
        {
            var currState = currPath.GetCurrState();
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                while (currState.searchingKeyword != null)
                {
                    currPath.Pop();
                    currState = currPath.GetCurrState();
                }
            }
            else
            {
                currPath.Push(currState.state, -1, SearchBox.Text);
            }

            if (currState.state == StateStack.State.TOPICS)
                UpdateTopics(ReloadTopics());
            else if (currState.state == StateStack.State.CORPUS)
                ReloadCorpus();
            else if (currState.state == StateStack.State.ARTICLES)
                ReloadArticles();
            UpdateView();
        }

        private void PacketButton_Click(object sender, RoutedEventArgs e)
        {
            PacketWindow dialog = new PacketWindow() { Owner = this };
            dialog.ShowDialog();
        }

        private void EditKeywordAliasContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            if (item.SKType != SKType.Simple)
            {
                return;
            }
            int FavoriteId = item.FavoriteId;
            SuperKeyword sk = metaData.superKeywords[FavoriteId];
            AliasEditDialog dialog = new AliasEditDialog()
            {
                Owner = this,
                Keyword = sk.keyword
            };
            sk.alias.ForEach(x => dialog.Aliases.Add(x));
            if (dialog.ShowDialog() ?? false)
            {
                SuperKeyword superKeyword = metaData.superKeywords[FavoriteId];
                superKeyword.alias = new List<string>(dialog.Aliases);
                metaData.superKeywords[FavoriteId] = superKeyword;
                MetaDataLoader.Save(this.metaData);
            }
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            StartReloadData();
        }

        private void CollectSingleArticleContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;

            int FavoriteId = metaData.superKeywords.FindIndex(x => x.keyword == "*");
            if (FavoriteId == -1)
            {
                SuperKeyword superKeyword = new SuperKeyword
                {
                    skType = SKType.Manual,
                    keyword = "*",
                    authors = new List<string>() { "*" },
                    alias = new List<string>(),
                    tids = new List<int>(),
                    kws = new List<List<int>>(),
                    read = -1,
                    subReads = new List<int>(),
                    groups = new List<List<string>>(),
                    groupedTids = new List<Group>(),
                    subSKs = new List<SuperKeyword>(),
                    noSKGTids = new List<Group>()
                };
                metaData.superKeywords.Add(superKeyword);
                FavoriteId = metaData.superKeywords.Count - 1;
            }

            string ThreadId = item.ThreadId;
            string SiteId = item.SiteId;
            int tid = metaData.threads.FindIndex(x => (x.siteId == SiteId) && (x.threadId == ThreadId));

            SuperKeyword sk = metaData.superKeywords[FavoriteId];
            if (!sk.tids.Contains(tid))
            {
                sk.tids.Add(tid);
            }
            sk.tids.Sort((x,y) => -metaData.threads[x].postTime.CompareTo(metaData.threads[y].postTime) );
            metaData.superKeywords[FavoriteId] = sk;

            MetaDataLoader.Save(metaData);
            StartResetUI();
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            ManualDownloadDialog dialog = new ManualDownloadDialog() { Owner = this };
            if (dialog.ShowDialog() ?? false)
            {
                string siteId = dialog.BBSId.ToString();
                string threadId = dialog.ThreadId;
                string u = "-u";
                string addTo = dialog.AddToSinglesTopic ? "-s" : "";
                ScriptDialog dialog2 = new ScriptDialog(ScriptDialog.ScriptId.DOWNLOAD_ONE_DETAIL, siteId, threadId, u, addTo) { Owner = this };
                if (dialog2.ShowDialog() ?? false)
                {
                    StartReloadData();
                }
            }
        }

        private void EditAuthorFollowupContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            if (item.SKType != SKType.Author)
            {
                return;
            }

            int FavoriteId = item.FavoriteId;
            SuperKeyword sk = metaData.superKeywords[FavoriteId];
            AuthorEditDialog dialog = new AuthorEditDialog() { Owner = this };
            dialog.Initialize(sk.authors, sk.subKeywords, sk.subReads, sk.tids.ConvertAll(x => metaData.threads[x].title));

            if (dialog.ShowDialog() ?? false)
            {
                List<dynamic> kwObjs = new List<dynamic>(dialog.Keywords);
                sk.authors = new List<string>(dialog.Authors);
                sk.subKeywords = kwObjs.ConvertAll<List<string>>(x => x.SubKeywords);
                sk.subReads = kwObjs.ConvertAll<int>(x => x.SubRead);
                metaData.superKeywords[FavoriteId] = sk;
                MetaDataLoader.Save(this.metaData);
            }
        }

        private void FollowAuthorContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            string author = item.Author;
            if (item.SKType == SKType.Author)
            {
                return;
            }

            if (metaData.superKeywords.Any(x => (x.skType == SKType.Author) && x.authors.Contains(author)))
            {
                return;
            }

            SuperKeyword superKeyword = new SuperKeyword
            {
                skType = SKType.Author,
                keyword = "*",
                authors = new List<string>() { author },
                alias = new List<string>(),
                tids = new List<int>(),
                kws = new List<List<int>>(),
                read = -1,
                subReads = new List<int>(),
                groups = new List<List<string>>(),
                groupedTids = new List<Group>()
            };
            metaData.superKeywords.Add(superKeyword);
            MetaDataLoader.Save(this.metaData);
            UpdateTopics(ReloadTopics());
            UpdateView();
        }

        private void UnfollowAuthorContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = sender as MenuItem;
            dynamic item = cmi.DataContext;
            if (item.SKType != SKType.Author)
            {
                return;
            }

            int FavoriteId = item.FavoriteId;
            metaData.superKeywords.RemoveAt(FavoriteId);
            MetaDataLoader.Save(this.metaData);
            UpdateTopics(ReloadTopics());
            UpdateView();
        }
    }
}
