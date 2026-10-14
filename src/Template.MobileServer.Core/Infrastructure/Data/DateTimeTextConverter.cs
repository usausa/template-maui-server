namespace Template.MobileServer.Infrastructure.Data;

using Smart.Data.Accessor.Converters;

// 日時は UTC の yyyy-MM-dd HH:mm:ss.fffffff の TEXT で保存する (Microsoft.Data.Sqlite の既定書式。文字列比較で範囲検索できる)
public sealed class DateTimeTextConverter : IValueConverter<string, DateTime>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss.fffffff";

    private static readonly string[] ParseFormats = ["yyyy-MM-dd HH:mm:ss.FFFFFFF", "yyyy-MM-ddTHH:mm:ss.FFFFFFF"];

    public static DateTime FromDb(string dbValue) =>
        DateTime.ParseExact(dbValue, ParseFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    public static string ToDb(DateTime clrValue)
    {
        var utc = clrValue.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(clrValue, DateTimeKind.Utc) : clrValue.ToUniversalTime();
        return utc.ToString(Format, CultureInfo.InvariantCulture);
    }
}
