namespace Template.MobileServer.Components;

using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using MudBlazor.Services;

using NSubstitute;

using Template.MobileServer.Services;
using Template.MobileServer.Web.Application.Context;
using Template.MobileServer.Web.Components;

public abstract class MudBlazorTestBase : BunitContext
{
    protected MudBlazorTestBase()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton(Substitute.For<IHostApplicationLifetime>());
        Services.AddSingleton<RefreshTimerFactory>();
        Services.AddSingleton<ApplicationServiceContextProvider>();
        Services.AddSingleton<ServiceContextProvider>(static p => p.GetRequiredService<ApplicationServiceContextProvider>());
        Services.AddScoped<BlazorServiceScope>();
    }
}
