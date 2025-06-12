namespace RdrLib.Model
{
	public enum FeedStatus : int
	{
		Ok = 0,
		Updating = 1,
		DoesNotExist = 2,
		Forbidden = 3,
		ParseFailed = 4,
		MovedCannotFollow = 5,
		Timeout = 6,
		Dns = 7,
		RateLimited = 8,
		InternetError = 9,
		Broken = 10,
		CertificateRevocationCheckFailed = 11,
		Other = 12,
		None = 13
	}
}
