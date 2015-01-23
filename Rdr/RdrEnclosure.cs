using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Rdr
{
    class RdrEnclosure : RdrBase
    {
        private readonly Uri _downloadLink = null;
        public Uri DownloadLink { get { return this._downloadLink; } }

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

        private string _duration = "00:00:00";
        public string Duration
        {
            get { return this._duration; }
            set
            {
                this._duration = value;
                OnNotifyPropertyChanged();
            }
        }

        private readonly int _fileSize = 0;
        public int FileSize { get { return this._fileSize; } }

        public RdrEnclosure(XElement e)
        {
            foreach (XAttribute each in e.Attributes())
            {
                if (each.Name.LocalName.Equals("url"))
                {
                    if (this._downloadLink == null)
                    {
                        this._downloadLink = HelperMethods.ConvertStringToUri(each.Value);
                    }
                }

                if (each.Name.LocalName.Equals("href"))
                {
                    if (this._downloadLink == null)
                    {
                        this._downloadLink = HelperMethods.ConvertStringToUri(each.Value);
                    }
                }

                if (each.Name.LocalName.Equals("length"))
                {
                    this._fileSize = HelperMethods.ConvertStringToInt32(each.Value);
                }
            }
        }

        private DelegateCommandAsync _downloadCommandAsync = null;
        public DelegateCommandAsync DownloadCommandAsync
        {
            get
            {
                if (this._downloadCommandAsync == null)
                {
                    this._downloadCommandAsync = new DelegateCommandAsync(new Func<Task>(DownloadAsync), (_) => { return true; });
                }

                return this._downloadCommandAsync;
            }
        }

        public async Task DownloadAsync()
        {
            if (this._downloadLink != null)
            {
                string link = this._downloadLink.AbsoluteUri;
                string localFilePath = DetermineLocalFilePath(link);

                WebClient wc = new WebClient();
                wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                wc.DownloadFileCompleted += wc_DownloadFileCompleted;

                await wc.DownloadFileTaskAsync(link, localFilePath);
            }
            else
            {
                this.ButtonText = "Error";
            }
        }

        private string DetermineLocalFilePath(string link)
        {
            int indexOfLastSlash = link.LastIndexOf(@"/");

            return string.Format(@"C:\Users\{0}\Documents\share\{1}", Environment.UserName, link.Substring(indexOfLastSlash));
        }

        private void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.ButtonText = string.Format("{0} %", e.ProgressPercentage);
        }

        private void wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            this.ButtonText = "Done";
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.GetType().ToString());
            sb.AppendLine(this.DownloadLink.AbsoluteUri);
            sb.AppendLine(this.Duration);
            sb.AppendLine(this.ButtonText);

            return sb.ToString();
        }
    }
}
