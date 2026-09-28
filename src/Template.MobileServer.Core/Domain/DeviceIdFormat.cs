namespace Template.MobileServer.Domain;

// 端末 ID の形式。テレメトリのファイル名に使うので英数字・'-'・'_' に限る
public static class DeviceIdFormat
{
    // API の入力検証用 (IsValid と同じ)
    public const string Pattern = "^[0-9A-Za-z_-]{1,64}$";

    public static bool IsValid([NotNullWhen(true)] string? value) =>
        value is { Length: > 0 and <= Length.DeviceId } && value.All(static x => Char.IsAsciiLetterOrDigit(x) || (x is '-' or '_'));
}
