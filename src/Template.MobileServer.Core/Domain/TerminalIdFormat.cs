namespace Template.MobileServer.Domain;

public static class TerminalIdFormat
{
    public const string Pattern = "^[0-9A-Za-z_-]{1,64}$";

    public static bool IsValid([NotNullWhen(true)] string? value) =>
        value is { Length: > 0 and <= Length.TerminalId } && value.All(static x => Char.IsAsciiLetterOrDigit(x) || (x is '-' or '_'));
}
