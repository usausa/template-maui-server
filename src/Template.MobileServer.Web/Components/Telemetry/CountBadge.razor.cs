namespace Template.MobileServer.Web.Components.Telemetry;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// 件数。1 以上は色付きのバッジ、0 は薄く出す
public sealed partial class CountBadge
{
    [Parameter]
    public int Count { get; set; }

    [Parameter]
    public Color BadgeColor { get; set; } = Color.Error;

    private bool HasCount => Count > 0;
}
