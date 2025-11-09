namespace Template.MobileServer.Web.Application.Components;

using Microsoft.JSInterop;

public static class JSRuntimeExtensions
{
    public static ValueTask ClickUrl(this IJSRuntime runtime, string url) =>
        runtime.InvokeVoidAsync("clickUrl", url);
}
