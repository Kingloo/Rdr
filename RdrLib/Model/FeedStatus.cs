namespace RdrLib.Model
{
	public enum FeedStatus
	{
		None,
		Ok,
		Updating,
		DoesNotExist,
		Forbidden,
		ParseFailed,
		MovedCannotFollow,
		Timeout,
		Dns,
		RateLimited,
		InternetError,
		Broken
	}
}
