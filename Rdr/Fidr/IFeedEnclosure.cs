using System;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Rdr.Fidr
{
    interface IFeedEnclosure
    {
        ContentType ContentType { get; set; }
        Uri Link { get; set; }
        int FileSize { get; set; }
        string Duration { get; set; }
        string ButtonText { get; set; }
        DelegateCommandAsync<IFeedEnclosure> DownloadEnclosureCommandAsync { get; }

        Task DownloadEnclosureAsync(IFeedEnclosure enclosure);
    }
}
