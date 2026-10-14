namespace Template.MobileServer.Web.Handlers;

using Grpc.Core;

public sealed class ServerInfoHandler : ServerInfo.ServerInfoBase
{
    private TimeProvider TimeProvider { get; }

    public ServerInfoHandler(TimeProvider timeProvider)
    {
        TimeProvider = timeProvider;
    }

    public override Task<ServerTimeReply> GetServerTime(ServerTimeRequest request, ServerCallContext context) =>
        Task.FromResult(new ServerTimeReply { Timestamp = TimeProvider.GetUtcNow().ToUnixTimeMilliseconds() });
}
