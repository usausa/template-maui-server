namespace Template.MobileServer.Web.Components;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;

using Template.MobileServer.Web.Infrastructure.Security;

public sealed partial class App
{
    private static readonly IComponentRenderMode InteractiveServerWithoutPrerender = new InteractiveServerRenderMode(prerender: false);

    [CascadingParameter]
    public HttpContext HttpContext { get; set; } = default!;

    [Inject]
    public required CspNonce Nonce { get; set; }

    // Static SSR for pages with [ExcludeFromInteractiveRouting]
    private IComponentRenderMode? PageRenderMode =>
        HttpContext.AcceptsInteractiveRouting() ? InteractiveServerWithoutPrerender : null;
}
