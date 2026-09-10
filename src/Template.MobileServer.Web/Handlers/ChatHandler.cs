namespace Template.MobileServer.Web.Handlers;

using System.Threading.Channels;

using Grpc.Core;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Template.MobileServer.Chat;
using Template.MobileServer.Web.Infrastructure.Chat;

// gRPCチャットサービス(双方向ストリーミング、JWT Bearer認証)
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class ChatHandler : ChatRoom.ChatRoomBase
{
    private readonly ChatService chatService;

    private readonly TimeProvider timeProvider;

    public ChatHandler(ChatService chatService, TimeProvider timeProvider)
    {
        this.chatService = chatService;
        this.timeProvider = timeProvider;
    }

    //--------------------------------------------------------------------------------
    // Bidirectional streaming
    //--------------------------------------------------------------------------------

    public override async Task Connect(IAsyncStreamReader<ChatMessage> requestStream, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
    {
        var cancellationToken = context.CancellationToken;

        // ユーザー名はJWTのNameクレーム由来(クライアント指定のuserは使用しない)
        var user = context.GetHttpContext().User.Identity?.Name ?? "unknown";

        // レスポンスヘッダーを即時送信する(履歴0件でもクライアントが接続確立を検知できるようにする)
        await context.WriteResponseHeadersAsync([]);

        // 他参加者の発言はチャネル経由で直列化してストリームへ書き込む
        var channel = Channel.CreateUnbounded<ChatEntry>(new UnboundedChannelOptions { SingleReader = true });
        void OnReceived(object? sender, ChatEntryEventArgs e) => channel.Writer.TryWrite(e.Entry);

        // 接続時に直近の履歴を送信する
        foreach (var entry in chatService.History)
        {
            await responseStream.WriteAsync(MapToMessage(entry), cancellationToken);
        }

        chatService.Received += OnReceived;
        var deliveryTask = DeliverAsync(channel.Reader, responseStream, cancellationToken);
        try
        {
            // 受信した発言を全参加者へブロードキャストする
            await foreach (var message in requestStream.ReadAllAsync(cancellationToken))
            {
                chatService.Publish(user, message.Text, timeProvider.GetUtcNow());
            }
        }
        catch (OperationCanceledException)
        {
            // クライアント切断
        }
        catch (IOException)
        {
            // 接続断
        }
        finally
        {
            chatService.Received -= OnReceived;
            channel.Writer.TryComplete();
            await deliveryTask;
        }
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    // チャネルの発言をレスポンスストリームへ配信する
    private static async Task DeliverAsync(ChannelReader<ChatEntry> reader, IServerStreamWriter<ChatMessage> responseStream, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var entry in reader.ReadAllAsync(cancellationToken))
            {
                await responseStream.WriteAsync(MapToMessage(entry), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // クライアント切断
        }
        catch (IOException)
        {
            // 接続断
        }
    }

    private static ChatMessage MapToMessage(ChatEntry entry) => new()
    {
        User = entry.User,
        Text = entry.Text,
        Timestamp = entry.Timestamp.ToUnixTimeMilliseconds()
    };
}
