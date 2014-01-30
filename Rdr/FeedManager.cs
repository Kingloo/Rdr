using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;

namespace Rdr
{
    class ItemsCollectionSwitchedEventArgs : EventArgs
    {
        public object Obj { get; private set; }

        public ItemsCollectionSwitchedEventArgs(object obj)
        {
            this.Obj = obj;
        }
    }

    class FeedManager : RdrBase
    {
        #region Events
        public event EventHandler<ItemsCollectionSwitchedEventArgs> ItemsCollectionSwitched;
        protected virtual void OnItemsCollectionSwitched(ItemsCollectionSwitchedEventArgs icsea)
        {
            EventHandler<ItemsCollectionSwitchedEventArgs> handler = this.ItemsCollectionSwitched;
            if (handler != null)
            {
                handler(this, icsea);
            }
        }
        #endregion

        #region Commands
        private DelegateCommandAsync refreshAllFeedsAsyncCommand = null;
        private DelegateCommandAsync refreshFeedAsyncCommand = null;
        private DelegateCommandAsync markAllItemsAsReadCommand = null;
        private DelegateCommand markItemAsReadCommand = null;
        private DelegateCommand goToFeedCommand = null;
        private DelegateCommand goToItemCommand = null;
        private DelegateCommand moveItemsToViewCommand = null;
        private DelegateCommand moveUnreadItemsToViewCommand = null;
        private DelegateCommand debugCommand = null;

        public DelegateCommandAsync RefreshAllFeedsAsyncCommand { get { return refreshAllFeedsAsyncCommand; } }
        public DelegateCommandAsync RefreshFeedAsyncCommand { get { return refreshFeedAsyncCommand; } }
        public DelegateCommandAsync MarkAllItemsAsReadCommand { get { return markAllItemsAsReadCommand; } }
        public DelegateCommand MarkItemAsReadCommand { get { return markItemAsReadCommand; } }
        public DelegateCommand GoToFeedCommand { get { return goToFeedCommand; } }
        public DelegateCommand GoToItemCommand { get { return goToItemCommand; } }
        public DelegateCommand MoveItemsToViewCommand { get { return moveItemsToViewCommand; } }
        public DelegateCommand MoveUnreadItemsToViewCommand { get { return moveUnreadItemsToViewCommand; } }
        public DelegateCommand DebugCommand { get { return debugCommand; } }
        #endregion

        #region Hidden
        private bool areMembersInitialized = false;
        private bool activity = false;
        private bool Activity
        {
            get
            {
                return this.activity;
            }
            set
            {
                this.activity = value;
                this.UIToggle();
            }
        }
        private readonly string feedsFile = string.Format(@"C:\Users\{0}\Documents\rssfeeds.xml", Environment.UserName);
        private readonly DispatcherTimer updateAllTimer = new DispatcherTimer();
        #endregion

        #region Visible
        private string _status = string.Empty;
        public string Status
        {
            get { return this._status; }
            set
            {
                this._status = value;
                OnPropertyChanged("Status");
            }
        }
        
        public ObservableCollection<Feed> Feeds { get; private set; }
        public ObservableCollection<FeedItem> Items { get; private set; }
        public ObservableCollection<FeedItem> UnreadItems { get; private set; }
        #endregion

        public FeedManager()
        {
            if (areMembersInitialized == false)
            {
                this.InitializeMembers();
                areMembersInitialized = true;
            }
        }

        private void InitializeMembers()
        {
            refreshAllFeedsAsyncCommand = new DelegateCommandAsync(new Func<object, Task>(RefreshAllFeedsAsync), canExecuteCommand);
            refreshFeedAsyncCommand = new DelegateCommandAsync(new Func<object, Task>(RefreshFeedAsync), canExecuteCommand);
            markAllItemsAsReadCommand = new DelegateCommandAsync(new Func<object, Task>(MarkAllItemsAsReadAsync), canExecuteCommand);
            markItemAsReadCommand = new DelegateCommand(MarkItemAsRead, canExecuteCommand);
            goToFeedCommand = new DelegateCommand(GoToFeed, canExecuteGoToCommand);
            goToItemCommand = new DelegateCommand(GoToItem, canExecuteGoToCommand);
            moveItemsToViewCommand = new DelegateCommand(MoveItemsToView, canExecuteCommand);
            moveUnreadItemsToViewCommand = new DelegateCommand(MoveUnreadItemsToView, canExecuteCommand);
            debugCommand = new DelegateCommand(Debug, canExecuteDebug);

            this.Feeds = new ObservableCollection<Feed>();
            this.Items = new ObservableCollection<FeedItem>();
            this.UnreadItems = new ObservableCollection<FeedItem>();

            this.updateAllTimer.Tick += updateAllTimer_Tick;
            this.updateAllTimer.Interval = new TimeSpan(0, 20, 0);
            this.updateAllTimer.IsEnabled = true;
        }

        private async void updateAllTimer_Tick(object sender, EventArgs e)
        {
            if (this.Activity == false)
            {
                await this.RefreshAllFeedsAsync(null);
            }
        }

        private void UIToggle()
        {
            this.RefreshAllFeedsAsyncCommand.RaiseCanExecuteChanged();
            this.RefreshFeedAsyncCommand.RaiseCanExecuteChanged();
            this.MarkAllItemsAsReadCommand.RaiseCanExecuteChanged();
        }

        public async Task LoadAsync()
        {
            this.Activity = true;
            this.Status = "loading feeds ...";

            if (File.Exists(this.feedsFile))
            {
                await LoadXmlUrlsFromFileAsync();
            }
            else
            {
                await WriteBasicFeedsFileAsync();
            }

            this.OnItemsCollectionSwitched(new ItemsCollectionSwitchedEventArgs(this.UnreadItems));

            this.Status = string.Format("{0} feeds loaded", this.Feeds.Count);
            this.Activity = false;
        }

        private async Task LoadXmlUrlsFromFileAsync()
        {
            XDocument xDoc = null;
            string feedsFileAsString = string.Empty;

            using (StreamReader sr = new StreamReader(this.feedsFile))
            {
                feedsFileAsString = await sr.ReadToEndAsync();
            }

            if (feedsFileAsString != string.Empty)
            {
                xDoc = XDocument.Parse(feedsFileAsString);

                if (xDoc != null)
                {
                    IEnumerable<XElement> feeds = xDoc.Root.Elements("feed");

                    foreach (XElement el in feeds)
                    {
                        Uri uri = null;

                        if (Uri.TryCreate(el.Attribute("xmlurl").Value, UriKind.Absolute, out uri))
                        {
                            this.Feeds.Add(new Feed(uri));
                        }
                    }
                }
            }
        }

        private async Task WriteBasicFeedsFileAsync()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<feeds>");
            sb.AppendLine("</feeds>");

            using (StreamWriter sw = File.CreateText(this.feedsFile))
            {
                await sw.WriteAsync(sb.ToString());
            }
        }

        public async Task RefreshAllFeedsAsync(object parameter)
        {
            this.Activity = true;

            if (this.Feeds.Count > 0)
            {
                foreach (Feed feed in this.Feeds)
                {
                    await RefreshFeedAsync(feed);
                }
            }

            this.Activity = false;
        }

        private async Task RefreshFeedAsync(object parameter)
        {
            Feed feed = parameter as Feed;

            this.Status = string.Format("updating {0}", feed.FeedTitle);

            HttpWebRequest req = BuildWebRequest(feed.XmlUrl);
            WebResponse resp = null;

            try
            {
                resp = await req.GetResponseAsync();
            }
            catch (WebException)
            {
                resp = null;
            }

            if (resp != null)
            {
                XmlReader reader = BuildXmlReader(resp.GetResponseStream());
                SyndicationFeed xmlFeed = null;

                try
                {
                    xmlFeed = await RetrieveFeedFromServer(reader);
                }
                catch (XmlException)
                {
                    xmlFeed = null;
                }
                catch (WebException)
                {
                    xmlFeed = null;
                }

                if (reader != null)
                {
                    reader.Close();
                }

                if (xmlFeed != null)
                {
                    ProcessReturnedXml(xmlFeed, feed);
                }
            }

            if (resp != null)
            {
                resp.Close();
            }

            this.Status = string.Empty;
        }

        private HttpWebRequest BuildWebRequest(Uri xmlUrl)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.CreateHttp(xmlUrl.AbsoluteUri);

            req.AllowAutoRedirect = true;
            req.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
            req.ServicePoint.ConnectionLimit = 3;
            req.KeepAlive = false;
            req.Method = "GET";
            req.ProtocolVersion = HttpVersion.Version10;
            req.Proxy = null;
            req.Referer = xmlUrl.DnsSafeHost;
            req.Timeout = 3000;
            req.UseDefaultCredentials = true;
            req.UserAgent = @"Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";

            return req;
        }

        private XmlReader BuildXmlReader(Stream stream)
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings()
            {
                CheckCharacters = true,
                CloseInput = true,
                DtdProcessing = System.Xml.DtdProcessing.Ignore,
                IgnoreComments = true,
                IgnoreWhitespace = true,
            };

            return XmlReader.Create(stream, readerSettings);
        }

        private Task<SyndicationFeed> RetrieveFeedFromServer(XmlReader reader)
        {
            return Task.Factory.StartNew<SyndicationFeed>(() =>
            {
                SyndicationFeed feed = SyndicationFeed.Load(reader);
                return feed;
            });
        }

        private void ProcessReturnedXml(SyndicationFeed xmlFeed, Feed feed)
        {
            List<FeedItem> itemsToAdd = new List<FeedItem>();

            if (feed.IsFirstLoad)
            {
                itemsToAdd = feed.FirstLoad(xmlFeed, 30);
            }
            else
            {
                itemsToAdd = feed.Load(xmlFeed);
            }

            //foreach (FeedItem feedItem in itemsToAdd)
            //{
            //    this.Items.Add(feedItem);
            //}

            foreach (FeedItem feedItem in itemsToAdd)
            {
                this.UnreadItems.Add(feedItem);
            }
        }

        private async Task MarkAllItemsAsReadAsync(object parameter)
        {
            this.Activity = true;

            this.UnreadItems.Clear();

            foreach (Feed feed in this.Feeds)
            {
                await feed.MarkAllItemsAsReadAsync();
            }

            this.Activity = false;
        }

        private void MarkItemAsRead(object parameter)
        {
            FeedItem feedItem = parameter as FeedItem;

            feedItem.Unread = false;
        }

        private void GoToItem(object parameter)
        {
            FeedItem feedItem = parameter as FeedItem;

            Uri uri = null;

            if (Uri.TryCreate(feedItem.Link, UriKind.Absolute, out uri))
            {
                Misc.OpenUrlInBrowser(uri.AbsoluteUri);

                feedItem.Unread = false;
            }
            else
            {
                MessageBox.Show("Cannot go to this item.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoToFeed(object parameter)
        {
            Feed feed = parameter as Feed;

            Misc.OpenUrlInBrowser(feed.XmlUrl.AbsoluteUri);
        }

        private void MoveItemsToView(object parameter)
        {
            this.Activity = true;

            this.Items.Clear();

            Feed feed = parameter as Feed;
            List<FeedItem> allItems = feed.FeedItems;

            if (allItems != null)
            {
                if (allItems.Count > 0)
                {
                    foreach (FeedItem item in allItems)
                    {
                        this.Items.Add(item);
                    }
                }
            }

            this.OnItemsCollectionSwitched(new ItemsCollectionSwitchedEventArgs(this.Items));

            //this.Items.Clear();

            //Feed feed = parameter as Feed;
            //List<FeedItem> allItems = feed.FeedItems;

            //if (allItems != null)
            //{
            //    if (allItems.Count > 0)
            //    {
            //        foreach (FeedItem item in allItems)
            //        {
            //            this.Items.Add(item);
            //        }
            //    }
            //}

            this.Activity = false;
        }

        private void MoveUnreadItemsToView(object parameter)
        {
            this.Activity = true;

            this.OnItemsCollectionSwitched(new ItemsCollectionSwitchedEventArgs(this.UnreadItems));

            //this.Items.Clear();

            //foreach (Feed feed in this.Feeds)
            //{
            //    foreach (FeedItem feedItem in feed.FeedItems)
            //    {
            //        if (feedItem.Unread)
            //        {
            //            this.Items.Add(feedItem);
            //        }
            //    }
            //}

            //this.Items.Clear();

            //foreach (FeedItem feedItem in this.unreadItems)
            //{
            //    this.Items.Add(feedItem);
            //}

            this.Activity = false;
        }

        private bool canExecuteCommand(object parameter)
        {
            if (this.Activity)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool canExecuteGoToCommand(object parameter)
        {
            return true;
        }

        private void Debug(object parameter)
        {
            Console.WriteLine(this.ToString());
        }

        private bool canExecuteDebug(object parameter)
        {
            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.feedsFile);
            sb.AppendLine(string.Format("Feeds: {0}", this.Feeds.Count));
            sb.AppendLine(string.Format("Items: {0}", this.Items.Count));

            return sb.ToString();
        }
    }
}
