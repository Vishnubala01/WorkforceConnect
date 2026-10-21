namespace WorkforceConnect.Application.Common;

/// <summary>Claim names used in the access token. Identity comes only from these, never from the request body.</summary>
public static class AppClaims
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Role = "role";
}
