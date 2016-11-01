using System;
using System.Text;
using System.Xml.Linq;

namespace Rdr
{
    public class RdrEnclosure : ViewModelBase
    {
        private readonly Uri _downloadLink = null;
        public Uri DownloadLink
        {
            get
            {
                return _downloadLink;
            }
        }

        private string _buttonText = "Download";
        public string ButtonText
        {
            get
            {
                return _buttonText;
            }
            set
            {
                if (_buttonText != value)
                {
                    _buttonText = value;

                    RaisePropertyChanged(nameof(ButtonText));
                }
            }
        }

        private string _duration = "00:00:00";
        public string Duration
        {
            get
            {
                return _duration;
            }
            set
            {
                if (_duration != value)
                {
                    _duration = value;

                    RaisePropertyChanged(nameof(Duration));
                }
            }
        }

        private readonly int _fileSize = 0;
        public int FileSize
        {
            get
            {
                return _fileSize;
            }
        }

        private bool _downloading = false;
        public bool Downloading
        {
            get
            {
                return _downloading;
            }
            set
            {
                if (_downloading != value)
                {
                    _downloading = value;

                    RaisePropertyChanged(nameof(Downloading));
                }
            }
        }

        public RdrEnclosure(XElement e)
        {
            if (e == null) { throw new ArgumentNullException(nameof(e)); }

            foreach (XAttribute each in e.Attributes())
            {
                if (each.Name.LocalName.Equals("url"))
                {
                    if (_downloadLink == null)
                    {
                        _downloadLink = HelperMethods.ConvertStringToUri(each.Value);
                    }
                }

                if (each.Name.LocalName.Equals("href"))
                {
                    if (_downloadLink == null)
                    {
                        _downloadLink = HelperMethods.ConvertStringToUri(each.Value);
                    }
                }

                if (each.Name.LocalName.Equals("length"))
                {
                    _fileSize = HelperMethods.ConvertStringToInt32(each.Value);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.GetType().ToString());
            sb.AppendLine(DownloadLink.AbsoluteUri);
            sb.AppendLine(Duration);
            sb.AppendLine(ButtonText);

            return sb.ToString();
        }
    }
}
