namespace RdrLib
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
        OtherInternetError,
        Broken
    }
}
