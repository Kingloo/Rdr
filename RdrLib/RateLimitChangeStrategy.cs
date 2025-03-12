namespace RdrLib
{
	public enum RateLimitChangeStrategy
	{
#pragma warning disable CA1720 // Identifier contains type name
		Double,
#pragma warning restore CA1720 // Identifier contains type name
		Triple,
		AddHour,
		AddDay
	}
}
