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
			BatchWhenLargerThan = 7,
			ChunkSize = 7,
			Interval = TimeSpan.FromSeconds(1)
		};

		public static BatchOptions Default { get => defaultBatchOptions; }
	}
}
