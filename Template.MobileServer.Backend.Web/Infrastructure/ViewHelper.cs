namespace Template.MobileServer.Backend.Web.Infrastructure;

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

    // --------------------------------------------------------------------------------
    // Status
    // --------------------------------------------------------------------------------

    public static string StatusColor(bool status)
    {
        return status ? Colors.Green.Accent4 : Colors.Gray.Default;
    }

    public static string BatteryIcon(double level)
    {
        if (level > 87.5)
        {
            return Icons.Material.Filled.BatteryFull;
        }
        if (level > 75)
        {
            return Icons.Material.Filled.Battery6Bar;
        }
        if (level > 62.5)
        {
            return Icons.Material.Filled.Battery5Bar;
        }
        if (level > 50)
        {
            return Icons.Material.Filled.Battery4Bar;
        }
        if (level > 37.5)
        {
            return Icons.Material.Filled.Battery3Bar;
        }
        if (level > 25)
        {
            return Icons.Material.Filled.Battery2Bar;
        }
        if (level > 12.5)
        {
            return Icons.Material.Filled.Battery1Bar;
        }
        return Icons.Material.Filled.Battery0Bar;
    }

    public static string BatteryColor(double level)
    {
        if (level > 50)
        {
            return Colors.Green.Accent4;
        }
        if (level > 20)
        {
            return Colors.Amber.Accent4;
        }
        return Colors.Red.Accent4;
    }
}
