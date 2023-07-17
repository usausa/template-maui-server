namespace Template.MobileServer.Frontend.Components;

public static class ViewExtensions
{
    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

    // --------------------------------------------------------------------------------
    // Basic
    // --------------------------------------------------------------------------------

    public static string Then(this bool value, string text) =>
        value ? text : string.Empty;

    public static string DateTime(this DateTime value) =>
        value.ToLocal().ToString("MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

    // --------------------------------------------------------------------------------
    // Status
    // --------------------------------------------------------------------------------

    private static DateTime ToLocal(this DateTime value) => TimeZoneInfo.ConvertTime(System.DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeZone);

    public static string FormatCount(this int value) => value.ToString("#,0", CultureInfo.InvariantCulture);

    // TODO
    //public static string Location(this StatusEntity value) => value.HasLocation() ? $"{value.LastLocationAt!.Value.ToLocal():HH:mm} : {value.Longitude:F3}, {value.Latitude:F3}" : string.Empty;

    //public static string MapLink(this StatusEntity value) => $"https://www.google.com/maps?q={value.Latitude},{value.Longitude}";
}
