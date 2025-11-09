namespace Template.MobileServer.Web.Components;

using MudBlazor;

public static class ViewHelper
{
    // --------------------------------------------------------------------------------
    // Functions
    // --------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Func<T, bool> FilterBy<T>(Func<T, bool> func) => func;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Func<T, object?> SortBy<T>(Func<T, object?> func) => func;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Func<T, string> TextBy<T>(Func<T, string> func) => func;

    // --------------------------------------------------------------------------------
    // Switch
    // --------------------------------------------------------------------------------

    public static Variant SelectedVariant(bool value) =>
        value ? Variant.Filled : Variant.Outlined;

    // --------------------------------------------------------------------------------
    // Password
    // --------------------------------------------------------------------------------

    public static InputType PasswordInputType(bool visible) =>
        visible ? InputType.Text : InputType.Password;

    public static string PasswordInputIcon(bool visible) =>
        visible ? Icons.Material.Filled.VisibilityOff : Icons.Material.Filled.Visibility;
}
