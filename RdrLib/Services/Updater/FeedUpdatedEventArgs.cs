using System;

namespace RdrLib.Services.Updater
{
	public record struct Count(int Value);
	public record struct Total(int Value);

	public class FeedUpdatedEventArgs : EventArgs
	{
		public Count Count { get; } = new Count(0);
		public Total Total { get; } = new Total(0);

		public FeedUpdatedEventArgs(Count count, Total total)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(count.Value);
			ArgumentOutOfRangeException.ThrowIfNegative(total.Value);

			Count = count;
			Total = total;
		}
	}
}