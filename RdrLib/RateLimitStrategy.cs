namespace RdrLib
{
	internal enum RateLimitLiftedStrategy
	{
		Reset,
		Maintain
	}

	internal enum RateLimitIncreaseStrategy
	{
		None,
		Double,
		AddHour,
		AddDay,
		Unknown
	}
}
