namespace Template.MobileServer.Web.Components;

using MudBlazor;

public static class Styles
{
    public static MudTheme Theme => new()
    {
        Palette = new PaletteLight
        {
            Primary = Colors.Blue.Default,
            Secondary = Colors.Cyan.Accent4,
            Tertiary = Colors.Teal.Accent4,
            Info = Colors.LightBlue.Default,
            AppbarBackground = Colors.Blue.Darken4
        }
    };

    public static MudTheme NoMenuTheme => new()
    {
        Palette = new PaletteLight
        {
            Primary = Colors.Blue.Default,
            Secondary = Colors.Green.Accent4,
            Tertiary = Colors.Teal.Accent4,
            Info = Colors.LightBlue.Default,
            AppbarBackground = Colors.Blue.Darken4
        },
        LayoutProperties = new LayoutProperties
        {
            AppbarHeight = "0"
        }
    };
}
