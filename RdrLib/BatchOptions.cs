using System;

namespace RdrLib
{
	public class BatchOptions
	{
		public int BatchWhenLargerThan { get; init; }
		public int ChunkSize { get; init; }
		public TimeSpan Interval { get; init; }

		private static readonly BatchOptions defaultBatchOptions = new BatchOptions
		{
			BatchWhenLargerThan = 5,
			ChunkSize = 5,
			Interval = TimeSpan.FromMilliseconds(500d)
		};

		public static BatchOptions Default { get => defaultBatchOptions; }
	}
}
