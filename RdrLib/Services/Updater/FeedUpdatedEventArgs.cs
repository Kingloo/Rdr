using System;

namespace RdrLib.Services.Updater
{
	public record struct Count(int Value);
	public record struct Total(int Value);

	public class FeedUpdatedEventArgs : EventArgs
	{
		private static readonly Count countZero = new Count(0);
		private static readonly Total countTotal = new Total(0);

		public Count Count { get; } = countZero;
		public Total Total { get; } = countTotal;

		public FeedUpdatedEventArgs(Count count, Total total)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(count.Value);
			ArgumentOutOfRangeException.ThrowIfNegative(total.Value);
			ArgumentOutOfRangeException.ThrowIfGreaterThan(count.Value, total.Value);

			Count = count;
			Total = total;
		}
	}
}