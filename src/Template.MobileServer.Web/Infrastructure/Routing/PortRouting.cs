namespace Template.MobileServer.Web.Infrastructure.Routing;

using Microsoft.AspNetCore.Routing.Matching;

public sealed record PortMetadata(int Port);

public static class PortEndpointConventionBuilderExtensions
{
    public static TBuilder RequirePort<TBuilder>(this TBuilder builder, int port)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(new PortMetadata(port));
        return builder;
    }
}

public sealed class PortMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
{
    public override int Order => 0;

    public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints) =>
        endpoints.Any(static e => e.Metadata.GetMetadata<PortMetadata>() is not null);

    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        for (var i = 0; i < candidates.Count; i++)
        {
            if (!candidates.IsValidCandidate(i))
            {
                continue;
            }

            var port = candidates[i].Endpoint.Metadata.GetMetadata<PortMetadata>()?.Port;
            if ((port is not null) && (port != httpContext.Connection.LocalPort))
            {
                candidates.SetValidity(i, false);
            }
        }

        return Task.CompletedTask;
    }
}
