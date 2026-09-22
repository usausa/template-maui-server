namespace Template.MobileServer.Web.Application;

public static class RequestHelper
{
    public static bool TryParse<TEnum>(string? value, TEnum defaultValue, out TEnum result)
        where TEnum : struct, Enum
    {
        if (String.IsNullOrEmpty(value))
        {
            result = defaultValue;
            return true;
        }

        result = default;
        return Char.IsLetter(value[0]) && Enum.TryParse(value, true, out result) && Enum.IsDefined(result);
    }

    public static TEnum Parse<TEnum>(string? value, TEnum defaultValue)
        where TEnum : struct, Enum =>
        TryParse(value, defaultValue, out var result) ? result : defaultValue;
}
