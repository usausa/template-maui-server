namespace Template.MobileServer.Web.Components;

public static class ViewExtensions
{
    // --------------------------------------------------------------------------------
    // Basic
    // --------------------------------------------------------------------------------

    public static string Then(this bool value, string text) =>
        value ? text : string.Empty;

    public static string DateTime(this DateTime value) =>
        value.ToString("MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
}
