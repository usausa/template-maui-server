namespace Template.MobileServer.Components;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using MudBlazor.Services;

using Template.MobileServer.Services;
using Template.MobileServer.Web.Application.Context;

public abstract class MudBlazorTestBase : BunitContext
{
    protected MudBlazorTestBase()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<AmbientServiceContextProvider>();
        Services.AddSingleton<ServiceContextProvider>(static p => p.GetRequiredService<AmbientServiceContextProvider>());
        Services.AddScoped<BlazorServiceScope>();
    }
}
