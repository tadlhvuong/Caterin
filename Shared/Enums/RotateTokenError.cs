namespace Shared.Enums
{
    public enum RotateTokenError
    {
        None = 0,
        TokenNotFound = 1,
        TokenRevoked,
        TokenInvalid,
        ReuseDetected ,
        TokenExpired,
        SessionInvalid,
        InvalidLifetime,
        ConcurrentConflict
    }
}
