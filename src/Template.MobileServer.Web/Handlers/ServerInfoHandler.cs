namespace Template.MobileServer.Web.Handlers;

using Grpc.Core;

public sealed class ServerInfoHandler : ServerInfo.ServerInfoBase
{
    private readonly TimeProvider timeProvider;

    public ServerInfoHandler(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    public override Task<ServerTimeReply> GetServerTime(ServerTimeRequest request, ServerCallContext context) =>
        Task.FromResult(new ServerTimeReply { Timestamp = timeProvider.GetUtcNow().ToUnixTimeMilliseconds() });
}
