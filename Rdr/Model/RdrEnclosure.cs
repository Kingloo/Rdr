using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Rdr.Model
{
    public class RdrEnclosure : ViewModelBase
    {
        #region Properties
        private readonly RdrFeedItem _parent = null;
        public RdrFeedItem Parent => _parent;

        private readonly Uri _downloadLink = null;
        public Uri DownloadLink => _downloadLink;

        private string _buttonText = "Download";
        public string ButtonText
        {
            get => _buttonText;
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
            get => _duration;
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
        public int FileSize => _fileSize;

        private bool _downloading = false;
        public bool Downloading
        {
            get => _downloading;
            set
            {
                if (_downloading != value)
                {
                    _downloading = value;

                    RaisePropertyChanged(nameof(Downloading));
                }
            }
        }
        #endregion

        public RdrEnclosure(RdrFeedItem parent, XElement element)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            if (element == null) { throw new ArgumentNullException(nameof(element)); }
            
            _downloadLink = GetDownloadLink(element.Attributes());
            _fileSize = GetFileSize(element.Attributes());
        }

        private static Uri GetDownloadLink(IEnumerable<XAttribute> elements)
        {
            var links = elements
                .Where(x => x.Name.LocalName.Equals("url", StringComparison.OrdinalIgnoreCase)
                || x.Name.LocalName.Equals("href", StringComparison.OrdinalIgnoreCase));

            if (links.Any())
            {
                foreach (var each in links)
                {
                    if (Uri.TryCreate(each.Value, UriKind.Absolute, out Uri uri))
                    {
                        return uri;
                    }
                }
            }

            return null;
        }

        private static int GetFileSize(IEnumerable<XAttribute> elements)
        {
            var lengths = elements
                .Where(x => x.Name.LocalName.Equals("length", StringComparison.OrdinalIgnoreCase));

            if (lengths.Any())
            {
                foreach (var each in lengths)
                {
                    if (Int32.TryParse(each.Value, out int result))
                    {
                        return result;
                    }
                }
            }

            return 0;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(GetType().Name);
            sb.AppendLine(DownloadLink.AbsoluteUri);
            sb.AppendLine(Duration);
            sb.AppendLine(ButtonText);

            return sb.ToString();
        }
    }
}
