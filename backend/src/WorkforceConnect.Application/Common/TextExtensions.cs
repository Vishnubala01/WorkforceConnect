namespace WorkforceConnect.Application.Common;

public static class TextExtensions
{
    /// <summary>Trims the value and turns blank input into null.</summary>
    public static string? NullIfBlank(this string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
