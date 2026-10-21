namespace WorkforceConnect.Application.Interfaces;

/// <summary>The authenticated caller, taken from the validated access token.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>Throws <see cref="Common.UnauthenticatedException"/> when there is no authenticated user.</summary>
    int UserId { get; }
}
