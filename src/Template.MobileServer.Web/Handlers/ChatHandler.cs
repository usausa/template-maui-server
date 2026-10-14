namespace Template.MobileServer.Web.Handlers;

using System.Threading.Channels;

using Grpc.Core;

using Template.MobileServer.Web.Services;

public sealed class ChatHandler : ChatRoom.ChatRoomBase
{
    private TimeProvider TimeProvider { get; }

    private ChatService ChatService { get; }

    public ChatHandler(TimeProvider timeProvider, ChatService chatService)
    {
        TimeProvider = timeProvider;
        ChatService = chatService;
    }

    //--------------------------------------------------------------------------------
    // Streaming
    //--------------------------------------------------------------------------------

    public override async Task Connect(IAsyncStreamReader<ChatMessage> requestStream, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
    {
        var cancellationToken = context.CancellationToken;

        // Connection detection
        await context.WriteResponseHeadersAsync([]);

        // Serialize messages to response stream using channel
        var channel = Channel.CreateUnbounded<ChatEntry>(new UnboundedChannelOptions { SingleReader = true });

        // Send recent history upon connection
        foreach (var entry in ChatService.History)
        {
            await responseStream.WriteAsync(MapToMessage(entry), cancellationToken);
        }

        ChatService.Received += OnReceived;
        var deliveryTask = DeliverAsync(channel.Reader, responseStream, cancellationToken);
        try
        {
            // Broadcast received messages to all participants (username is client-specified)
            await foreach (var message in requestStream.ReadAllAsync(cancellationToken))
            {
                ChatService.Publish(String.IsNullOrEmpty(message.User) ? "unknown" : message.User, message.Text, TimeProvider.GetUtcNow());
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }
        catch (IOException)
        {
            // Connection lost
        }
        finally
        {
            ChatService.Received -= OnReceived;
            channel.Writer.TryComplete();
            await deliveryTask;
        }

        void OnReceived(object? sender, ChatEntryEventArgs e) => channel.Writer.TryWrite(e.Entry);
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

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
            // Client disconnected
        }
        catch (IOException)
        {
            // Connection lost
        }
    }

    private static ChatMessage MapToMessage(ChatEntry entry) => new()
    {
        User = entry.User,
        Text = entry.Text,
        Timestamp = entry.Timestamp.ToUnixTimeMilliseconds()
    };
}
