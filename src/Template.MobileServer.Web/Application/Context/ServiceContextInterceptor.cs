namespace Template.MobileServer.Web.Application.Context;

using Grpc.Core;
using Grpc.Core.Interceptors;

public sealed class ServiceContextInterceptor : Interceptor
{
    private readonly ApplicationServiceContextProvider provider;

    public ServiceContextInterceptor(ApplicationServiceContextProvider provider)
    {
        this.provider = provider;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        using var scope = Begin(context);
        return await continuation(request, context);
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        using var scope = Begin(context);
        return await continuation(requestStream, context);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        using var scope = Begin(context);
        await continuation(request, responseStream, context);
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        using var scope = Begin(context);
        await continuation(requestStream, responseStream, context);
    }

    private IDisposable Begin(ServerCallContext context) =>
        provider.Begin(() => HttpServiceContext.Create(context.GetHttpContext()));
}
