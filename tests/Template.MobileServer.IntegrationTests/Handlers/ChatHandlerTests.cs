namespace Template.MobileServer.Handlers;

using Google.Protobuf;

using Grpc.Core;
using Grpc.Net.Client;

using Template.MobileServer.Web.Handlers;

public sealed class ChatHandlerTests : IClassFixture<ServerFixture>
{
    // サーバーは受け側のコードだけを生成するので、呼び出しの定義はここで組み立てる
    private static readonly Marshaller<ChatMessage> ChatMessageMarshaller = Marshallers.Create(static x => x.ToByteArray(), ChatMessage.Parser.ParseFrom);

    private static readonly Method<ChatMessage, ChatMessage> ConnectMethod = new(MethodType.DuplexStreaming, "chat.ChatRoom", "Connect", ChatMessageMarshaller, ChatMessageMarshaller);

    private ServerFixture Fixture { get; }

    public ChatHandlerTests(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    // 発言は送り手にも届く (時刻はサーバーが付ける)
    [Fact]
    public async Task ConnectBroadcastsMessage()
    {
        // Arrange
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        using var channel = GrpcChannel.ForAddress(Fixture.GrpcAddress);
        using var call = channel.CreateCallInvoker().AsyncDuplexStreamingCall(ConnectMethod, null, new CallOptions(cancellationToken: timeout.Token));
        await call.ResponseHeadersAsync;
        var text = $"it-{Guid.NewGuid():N}";
        var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        await call.RequestStream.WriteAsync(new ChatMessage { User = "user-1", Text = text }, timeout.Token);
        ChatMessage? received = null;
        await foreach (var message in call.ResponseStream.ReadAllAsync(timeout.Token))
        {
            if (message.Text == text)
            {
                received = message;
                break;
            }
        }
        await call.RequestStream.CompleteAsync();

        // Assert
        Assert.NotNull(received);
        Assert.Equal("user-1", received.User);
        Assert.InRange(received.Timestamp, before, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }
}
