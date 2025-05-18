using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RdrLib.Helpers;
using RdrLib.Model;

namespace RdrLib.Services.Loader
{
	public class FeedLoader
	{
		public static readonly Encoding DefaultEncoding = new UTF8Encoding(
			encoderShouldEmitUTF8Identifier: false,
			throwOnInvalidBytes: true);

		public FeedLoader() { }

		public async Task<IList<Feed>> LoadAsync(Stream stream, Encoding encoding, CancellationToken cancellationToken)
		{
			using StreamReader sr = new StreamReader(stream, encoding);

			List<Feed> feeds = new List<Feed>(capacity: 100);

			string? line = string.Empty;

			while ((line = await sr.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
			{
				if (line.StartsWith('#') == false)
				{
					if (Uri.TryCreate(line, UriKind.Absolute, out Uri? uri))
					{
						if (FeedHelpers.TryCreate(uri, out Feed? feed))
						{
							feeds.Add(feed);
						}
					}
				}
			}

			return feeds;
		}
	}
}
