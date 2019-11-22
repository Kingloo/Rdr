using System;

namespace RdrLib.Model
{
    public class Enclosure : BindableBase
    {
        public Uri DownloadLink { get; } = null;
        public Int64 Size { get; } = 0L;

        public Enclosure(Uri uri, Int64 size)
        {
            DownloadLink = uri ?? throw new ArgumentNullException(nameof(uri));
            Size = size;
        }
    }
}
