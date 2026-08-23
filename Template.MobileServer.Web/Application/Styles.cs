namespace Template.MobileServer.Web.Application;

using MudBlazor;

public static class Styles
{
    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Colors.Blue.Darken2,
            AppbarBackground = Colors.Blue.Darken2
        },
        PaletteDark = new PaletteDark()
    };
}
