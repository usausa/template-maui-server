namespace Template.MobileServer.Web.Components;

public static class Styles
{
    public static MudTheme Theme => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Colors.Blue.Default,
            Secondary = Colors.Cyan.Accent4,
            Info = Colors.Teal.Default,
            AppbarBackground = Colors.Blue.Darken4
        },
        PaletteDark = new PaletteDark
        {
            Primary = Colors.Blue.Default,
            Secondary = Colors.Cyan.Accent4,
            Info = Colors.Teal.Default,
            AppbarBackground = Colors.Blue.Darken4
        }
    };

    public static MudTheme NoMenuTheme => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Colors.Blue.Default,
            Secondary = Colors.Cyan.Accent4,
            Info = Colors.Teal.Default,
            AppbarBackground = Colors.Blue.Darken4
        },
        PaletteDark = new PaletteDark
        {
            Primary = Colors.Blue.Default,
            Secondary = Colors.Cyan.Accent4,
            Info = Colors.Teal.Default,
            AppbarBackground = Colors.Blue.Darken4
        },
        LayoutProperties = new LayoutProperties
        {
            AppbarHeight = "0"
        }
    };
}
