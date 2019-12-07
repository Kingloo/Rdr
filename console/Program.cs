using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RdrLib;
using RdrLib.Model;

namespace console
{
    public static class Program
    {
        public static async Task<int> Main()
        {
            //Uri uri = new Uri("https://feeds.twit.tv/ww_video_hd.xml");
            // Uri uri = new Uri("https://www.spreaker.com/show/3240193/episodes/feed");
            // Uri uri = new Uri("https://rss.art19.com/conan-obrien");
            Uri uri = new Uri("https://www.giantbomb.com/podcast-xml/devcast");
            // Uri uri = new Uri("https://www.youtube.com/feeds/videos.xml?channel_id=UCftcLVz-jtPXoH3cWUUDwYw");
            // Uri uri = new Uri("https://www.anandtech.com/rss/");
            // Uri uri = new Uri("https://utcc.utoronto.ca/~cks/space/blog/?atom");

            Feed feed = new Feed(uri);

            RdrService service = new RdrService();

            service.Add(feed);

            Console.WriteLine(feed.Status.ToString());

            await service.UpdateAsync(feed);

            // foreach (Item item in feed.Items)
            // {
            //     Console.WriteLine($"{item.Name}, {(item.Link?.AbsoluteUri ?? "\tNO LINK")}");
            // }
            
            if (feed.Items.Count > 0)
            {
                Item item = feed.Items.First();

                if (item.Enclosure is Enclosure enclosure)
                {
                    if (enclosure.Link != null)
                    {
                        string userProfileDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        string filename = enclosure.Link.Segments.LastOrDefault() ?? "unknown";

                        string path = Path.Combine(userProfileDirectory, "share", filename);

                        var progress = new Progress<DownloadProgress>(onProgress);

                        Console.WriteLine($"downloading {enclosure.Link.AbsoluteUri} to {path}");

                        DownloadResult result = await service.DownloadEnclosureAsync(enclosure, path, progress);

                        Console.WriteLine(result.ToString());
                    }
                }
            }

            return 0;
        }

        private static void onProgress(DownloadProgress obj)
        {
            if (obj.ContentLength.HasValue)
            {
                decimal current = Convert.ToDecimal(obj.TotalBytesReceived);
                decimal total = Convert.ToDecimal(obj.ContentLength.Value);

                decimal percent = current / total;

                Console.WriteLine(percent.ToString(GetPercentFormat()));
            }
            else
            {
                Console.WriteLine($"downloaded {obj.TotalBytesReceived} bytes");
            }
        }

        private static string GetPercentFormat()
        {
            var cc = CultureInfo.CurrentCulture;

            string separator = cc.NumberFormat.PercentDecimalSeparator;
            string symbol = cc.NumberFormat.PercentSymbol;

            return String.Format(cc, "00{0}0 {1}", separator, symbol);
        }
    }
}
