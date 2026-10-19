namespace Template.MobileServer.Web.Components.Telemetry;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class CountBadge
{
    [Parameter]
    public int Count { get; set; }

    [Parameter]
    public Color BadgeColor { get; set; } = Color.Error;

    private bool HasCount => Count > 0;
}
