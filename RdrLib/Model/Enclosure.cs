using System;

namespace RdrLib.Model
{
    public class Enclosure : BindableBase
    {
        public Uri? Link { get; } = null;
        public Int64 Size { get; } = 0L;

        public Enclosure(Uri? uri, Int64 size)
        {
            Link = uri;
            Size = size;
        }
    }
}
