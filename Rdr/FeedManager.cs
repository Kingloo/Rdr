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
    class FeedManager : RdrBase
    {
        #region Commands
        private DelegateCommandAsync refreshAllFeedsAsyncCommand = null;
        private DelegateCommandAsync refreshFeedAsyncCommand = null;
        private DelegateCommand markAllItemsAsReadCommand = null;
        private DelegateCommand markItemAsReadCommand = null;
        private DelegateCommand goToFeedCommand = null;
        private DelegateCommand goToItemCommand = null;
        private DelegateCommand moveItemsToViewCommand = null;
        private DelegateCommand moveUnreadItemsToViewCommand = null;
        private DelegateCommand debugCommand = null;

        public DelegateCommandAsync RefreshAllFeedsAsyncCommand
        {
            get
            {
                if (refreshAllFeedsAsyncCommand != null)
                {
                    return refreshAllFeedsAsyncCommand;
                }

                refreshAllFeedsAsyncCommand = new DelegateCommandAsync(RefreshAllFeedsAsyncAction, canExecuteCommand);
                return refreshAllFeedsAsyncCommand;
            }
        }
        public DelegateCommandAsync RefreshFeedAsyncCommand
        {
            get
            {
                if (refreshFeedAsyncCommand != null)
                {
                    return refreshFeedAsyncCommand;
                }

                refreshFeedAsyncCommand = new DelegateCommandAsync(RefreshFeedAsyncAction, canExecuteCommand);
                return refreshFeedAsyncCommand;
            }
        }
        public DelegateCommand MarkAllItemsAsReadCommand
        {
            get
            {
                if (markAllItemsAsReadCommand != null)
                {
                    return markAllItemsAsReadCommand;
                }

                markAllItemsAsReadCommand = new DelegateCommand(MarkAllItemsAsRead, canExecuteCommand);
                return markAllItemsAsReadCommand;
            }
        }
        public DelegateCommand MarkItemAsReadCommand
        {
            get
            {
                if (markItemAsReadCommand != null)
                {
                    return markItemAsReadCommand;
                }

                markItemAsReadCommand = new DelegateCommand(MarkItemAsRead, canExecuteCommand);
                return markItemAsReadCommand;
            }
        }
        public DelegateCommand GoToFeedCommand
        {
            get
            {
                if (goToFeedCommand != null)
                {
                    return goToFeedCommand;
                }

                goToFeedCommand = new DelegateCommand(GoToFeed, canExecuteGoToCommand);
                return goToFeedCommand;
            }
        }
        public DelegateCommand GoToItemCommand
        {
            get
            {
                if (goToItemCommand != null)
                {
                    return goToItemCommand;
                }

                goToItemCommand = new DelegateCommand(GoToItem, canExecuteGoToCommand);
                return goToItemCommand;
            }
        }
        public DelegateCommand MoveItemsToViewCommand
        {
            get
            {
                if (moveItemsToViewCommand != null)
                {
                    return moveItemsToViewCommand;
                }

                moveItemsToViewCommand = new DelegateCommand(MoveItemsToView, canExecuteCommand);
                return moveItemsToViewCommand;
            }
        }
        public DelegateCommand MoveUnreadItemsToViewCommand
        {
            get
            {
                if (moveUnreadItemsToViewCommand != null)
                {
                    return moveUnreadItemsToViewCommand;
                }

                moveUnreadItemsToViewCommand = new DelegateCommand(MoveUnreadItemsToView, canExecuteCommand);
                return moveUnreadItemsToViewCommand;
            }
        }
        public DelegateCommand DebugCommand
        {
            get
            {
                if (debugCommand != null)
                {
                    return debugCommand;
                }

                debugCommand = new DelegateCommand(Debug, canExecuteDebug);
                return debugCommand;
            }
        }
        #endregion

        #region Hidden
        private bool inactive = true;
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
        #endregion

        public FeedManager()
        {
            this.Feeds = new ObservableCollection<Feed>();
            this.Items = new ObservableCollection<FeedItem>();

            this.updateAllTimer.Tick += updateAllTimer_Tick;
            this.updateAllTimer.Interval = new TimeSpan(0, 20, 0);
            this.updateAllTimer.IsEnabled = true;
        }

        private async void updateAllTimer_Tick(object sender, EventArgs e)
        {
            if (this.inactive)
            {
                await this.RefreshAllFeedsAsync();
            }
        }

        private void UIToggle(bool turningUIOn)
        {
            if (turningUIOn)
            {
                this.inactive = true;
            }
            else
            {
                this.inactive = false;
            }

            Disp.Invoke(new Action(
                delegate()
                {
                    CommandManager.InvalidateRequerySuggested();
                }));
        }

        public Task LoadAsync()
        {
            this.UIToggle(false);

            this.Status = "loading feeds ...";

            return Task.Factory.StartNew(new Action(
                delegate()
                {
                    if (File.Exists(this.feedsFile))
                    {
                        LoadXmlUrlsFromFile();
                    }
                    else
                    {
                        WriteBasicFeedsFile();
                    }

                    this.Status = string.Format("{0} feeds loaded", this.Feeds.Count);

                    this.UIToggle(true);
                }));
        }

        private void LoadXmlUrlsFromFile()
        {
            XDocument xDoc = null;

            using (StreamReader sr = new StreamReader(this.feedsFile))
            {
                xDoc = XDocument.Load(sr.BaseStream);
            }

            if (xDoc != null)
            {
                IEnumerable<XElement> feeds = xDoc.Root.Elements("feed");

                foreach (XElement el in feeds)
                {
                    Uri uri = null;

                    if (Uri.TryCreate(el.Attribute("xmlurl").Value, UriKind.Absolute, out uri))
                    {
                        Disp.Invoke(new Action(
                            delegate()
                            {
                                this.Feeds.Add(new Feed(uri));
                            }));
                    }
                }
            }
        }

        private async void WriteBasicFeedsFile()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<feeds>");
            sb.AppendLine("</feeds>");

            using (StreamWriter sw = File.CreateText(this.feedsFile))
            {
                //sw.Write(sb.ToString());
                await sw.WriteAsync(sb.ToString());
            }
        }

        private void RefreshAllFeedsTest(object parameter)
        {
            this.UIToggle(false);

            foreach (Feed feed in this.Feeds)
            {
                RefreshFeedTest(feed);
            }

            this.UIToggle(true);
        }

        private async void RefreshFeedTest(Feed feed)
        {
            this.Status = string.Format("updating {0}", feed.Title);

            HttpWebRequest req = BuildWebRequest(feed.XmlUrl);
            WebResponse resp = null;
            XmlReader reader = null;
            SyndicationFeed xmlFeed = null;

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
                reader = BuildXmlReader(resp.GetResponseStream());

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

                if (xmlFeed != null)
                {
                    ProcessReturnedXml(xmlFeed, ref feed);
                }

                reader.Close();
                resp.Close();
            }
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

        private void RefreshSpecificFeed(object parameter)
        {
            this.UIToggle(false);

            RefreshFeedTest(parameter as Feed);

            this.UIToggle(true);
        }




        private Task RefreshAllFeedsAsync()
        {
            return Task.Factory.StartNew(RefreshAllFeedsAsyncAction, null);
        }

        public void RefreshAllFeedsAsyncAction(object parameter)
        {
            this.UIToggle(false);

            foreach (Feed feed in this.Feeds)
            {
                RefreshFeed(feed);
            }

            this.Status = string.Format("{0} items", this.Items.Count);

            this.UIToggle(true);
        }

        private void RefreshFeedAsyncAction(object parameter)
        {
            if (!(parameter is Feed))
            {
                throw new ArgumentException("FeedManager.cs -> RefreshFeedAsyncAction(object parameter) -> parameter must be Rdr.Feed");
            }

            this.UIToggle(false);

            Feed feed = parameter as Feed;

            RefreshFeed(feed);

            this.Status = string.Format("{0} update finished", feed.Title);

            this.UIToggle(true);
        }

        private void RefreshFeed(Feed feed)
        {
            this.Status = string.Format("updating {0}", feed.Title);

            HttpWebRequest req = BuildWebRequest(feed.XmlUrl);

            WebResponse resp = null;
            XmlReader reader = null;
            SyndicationFeed xmlFeed = null;

            try
            {
                resp = req.GetResponse();
            }
            catch (WebException)
            {
                resp = null;
            }

            if (resp != null)
            {
                reader = BuildXmlReader(resp.GetResponseStream());

                try
                {
                    xmlFeed = SyndicationFeed.Load(reader);
                }
                catch (XmlException)
                {
                    xmlFeed = null;
                }
                catch (WebException)
                {
                    xmlFeed = null;
                }

                if (xmlFeed != null)
                {
                    ProcessReturnedXml(xmlFeed, ref feed);
                }

                reader.Close();
                resp.Close();
            }
        }

        

        private void ProcessReturnedXml(SyndicationFeed xmlFeed, ref Feed feed)
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

            foreach (FeedItem feedItem in itemsToAdd)
            {
                Disp.Invoke(new Action(
                    delegate()
                    {
                        this.Items.Add(feedItem);
                    }));
            }
        }

        private void MarkAllItemsAsRead(object parameter)
        {
            this.Items.Clear();

            foreach (Feed feed in this.Feeds)
            {
                feed.MarkAllItemsAsRead();
            }

            this.Status = string.Empty;
        }

        private void MarkItemAsRead(object parameter)
        {
            FeedItem feedItem = parameter as FeedItem;

            feedItem.Unread = false;
        }

        private void GoToItem(object parameter)
        {
            if (!(parameter is FeedItem))
            {
                throw new ArgumentException("FeedManager.cs -> GoToItem(object parameter) -> parameter must be FeedItem");
            }

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
            if (!(parameter is Feed))
            {
                throw new ArgumentException("FeedManager.cs -> GoToFeed(object parameter) -> parameter must be Feed");
            }

            Feed feed = parameter as Feed;

            Misc.OpenUrlInBrowser(feed.XmlUrl.AbsoluteUri);
        }

        private void MoveItemsToView(object parameter)
        {
            if (!(parameter is Feed))
            {
                throw new ArgumentException("FeedManager.cs -> MoveAllAFeedsItemsToView(object parameter) -> parameter must be Rdr.Feed");
            }

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
        }

        private void MoveUnreadItemsToView(object parameter)
        {
            lock (this.Items)
            {
                this.Items.Clear();

                foreach (Feed feed in this.Feeds)
                {
                    foreach (FeedItem feedItem in feed.FeedItems)
                    {
                        if (feedItem.Unread)
                        {
                            this.Items.Add(feedItem);
                        }
                    }
                }
            }
        }

        private bool canExecuteCommand(object parameter)
        {
            //if (this.inactive)
            //{
            //    return true;
            //}

            return false;
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
