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
