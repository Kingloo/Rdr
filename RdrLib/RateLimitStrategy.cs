namespace RdrLib
{
	public enum RateLimitLiftedStrategy
	{
		Reset,
		Maintain,
		None
	}

	public enum RateLimitIncreaseStrategy
	{
		None,
#pragma warning disable CA1720 // doesn't like that Double is a type name
		Double,
#pragma warning restore CA1720
		AddHour,
		AddDay,
		Unknown
	}
}
