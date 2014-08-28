using System;
using System.ComponentModel;
using System.Net;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Rdr.Fidr
{
    abstract class FeedEnclosure : IFeedEnclosure, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnNotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChangedEventHandler pceh = this.PropertyChanged;
            if (pceh != null)
            {
                pceh(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private ContentType _contentType = new ContentType { MediaType = "application/unknown" };
        public ContentType ContentType
        {
            get { return this._contentType; }
            set { this._contentType = value; }
        }

        private Uri _link = null;
        public Uri Link
        {
            get { return this._link; }
            set { this._link = value; }
        }

        private int _fileSize = 0;
        public int FileSize
        {
            get { return this._fileSize; }
            set { this._fileSize = value; }
        }

        private string _duration = "00:00:00";
        public string Duration
        {
            get { return this._duration; }
            set { this._duration = value; }
        }

        private string _buttonText = "Download";
        public string ButtonText
        {
            get { return this._buttonText; }
            set
            {
                this._buttonText = value;
                OnNotifyPropertyChanged();
            }
        }

        private DelegateCommandAsync<IFeedEnclosure> _downloadEnclosureCommandAsync = null;
        public DelegateCommandAsync<IFeedEnclosure> DownloadEnclosureCommandAsync
        {
            get
            {
                if (this._downloadEnclosureCommandAsync == null)
                {
                    this._downloadEnclosureCommandAsync = new DelegateCommandAsync<IFeedEnclosure>(new Func<IFeedEnclosure, Task>(DownloadEnclosureAsync), canExecute);
                }

                return this._downloadEnclosureCommandAsync;
            }
        }

        public async Task DownloadEnclosureAsync(IFeedEnclosure enclosure)
        {
            if (enclosure.Link != null)
            {
                WebClient wc = new WebClient();
                wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                wc.DownloadFileCompleted += wc_DownloadFileCompleted;

                string link = enclosure.Link.AbsoluteUri;
                string localFilePath = DetermineLocalFilePath(link);

                await wc.DownloadFileTaskAsync(link, localFilePath);
            }
        }

        private void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.ButtonText = string.Format("{0} %", e.ProgressPercentage);
        }

        private void wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            this.ButtonText = "Done";
        }

        private string DetermineLocalFilePath(string link)
        {
            int indexOfLastSlash = link.LastIndexOf(@"/");

            return string.Format(@"C:\Users\{0}\Documents\share\{1}", Environment.UserName, link.Substring(indexOfLastSlash));
        }

        private bool canExecute(IFeedEnclosure _)
        {
            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.GetType().ToString());
            sb.AppendLine(string.Format("ContentType: {0}", this.ContentType.MediaType));
            sb.AppendLine(string.Format("Link: {0}", this.Link.AbsoluteUri));
            sb.AppendLine(string.Format("FileSize: {0}", this.FileSize));
            sb.AppendLine(string.Format("Duration: {0}", this.Duration));

            return sb.ToString();
        }
    }
}
