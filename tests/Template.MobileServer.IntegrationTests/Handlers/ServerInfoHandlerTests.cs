namespace Template.MobileServer.Handlers;

using Google.Protobuf;

using Grpc.Core;
using Grpc.Net.Client;

using Template.MobileServer.Web.Handlers;

public sealed class ServerInfoHandlerTests : IClassFixture<ServerFixture>
{
    private static readonly Method<ServerTimeRequest, ServerTimeReply> GetServerTimeMethod = new(
        MethodType.Unary,
        "info.ServerInfo",
        "GetServerTime",
        Marshallers.Create(static x => x.ToByteArray(), ServerTimeRequest.Parser.ParseFrom),
        Marshallers.Create(static x => x.ToByteArray(), ServerTimeReply.Parser.ParseFrom));

    private ServerFixture Fixture { get; }

    public ServerInfoHandlerTests(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task GetServerTimeReturnsUnixMilliseconds()
    {
        // Arrange
        using var channel = GrpcChannel.ForAddress(Fixture.GrpcAddress);
        var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        using var call = channel.CreateCallInvoker().AsyncUnaryCall(GetServerTimeMethod, null, new CallOptions(cancellationToken: TestContext.Current.CancellationToken), new ServerTimeRequest());
        var reply = await call.ResponseAsync;

        // Assert
        Assert.InRange(reply.Timestamp, before, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    [Fact]
    public async Task GetServerTimeIsUnimplementedOnOtelPort()
    {
        // Arrange
        using var channel = GrpcChannel.ForAddress(Fixture.OtelAddress);

        // Act
        using var call = channel.CreateCallInvoker().AsyncUnaryCall(GetServerTimeMethod, null, new CallOptions(cancellationToken: TestContext.Current.CancellationToken), new ServerTimeRequest());
        var exception = await Assert.ThrowsAsync<RpcException>(() => call.ResponseAsync);

        // Assert
        Assert.Equal(StatusCode.Unimplemented, exception.StatusCode);
    }
}
