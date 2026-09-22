namespace Template.MobileServer.ChatClient.Chat;

using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Channels;

using Grpc.Core;
using Grpc.Net.Client;

using Smart.Mapper;

using Template.MobileServer.Web.Handlers;

// gRPCチャット接続クライアント(プラットフォーム非依存・MAUIへそのまま移植可能)
// - gRPC双方向ストリームに接続する(認証なし。ユーザー名はメッセージで送る)
// - 切断・接続失敗時は指数バックオフで自動再接続する
// - 送信はキュー経由で直列化し、切断中の送信は再接続後に配送される
internal sealed partial class ChatClient : IAsyncDisposable
{
    // 再接続バックオフ(初期1秒、最大30秒)
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

    private readonly Channel<string> sendChannel = Channel.CreateUnbounded<string>();

    private readonly TaskCompletionSource firstAttemptCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly string grpcUrl;

    private readonly string userId;

    private CancellationTokenSource? cancellationTokenSource;

    private Task? runTask;

    public ChatConnectionState State { get; private set; }

    // 受信通知(バックグラウンドスレッドから発火する。UIスレッドへのマーシャリングは呼び出し側で行う)
    public event EventHandler<ChatMessageEventArgs>? MessageReceived;

    // 接続状態変化通知(バックグラウンドスレッドから発火する)
    public event EventHandler<ChatStateEventArgs>? StateChanged;

    public ChatClient(string grpcUrl, string userId)
    {
        this.grpcUrl = grpcUrl;
        this.userId = userId;
    }

    public ValueTask DisposeAsync() => DisconnectAsync();

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    // 接続を開始する(戻りのTaskは初回接続の成否確定まで待機。以降は内部で自動再接続)
    public Task ConnectAsync()
    {
        if (runTask is null)
        {
            cancellationTokenSource = new CancellationTokenSource();
            runTask = RunAsync(cancellationTokenSource.Token);
        }

        return firstAttemptCompletion.Task;
    }

    // 送信キューへ投入する(切断中も保持され、再接続後に配送される)
    public ValueTask SendAsync(string text) =>
        sendChannel.Writer.WriteAsync(text);

    // 切断する(再接続ループを停止)
    public async ValueTask DisconnectAsync()
    {
        if (cancellationTokenSource is null)
        {
            return;
        }

        await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        if (runTask is not null)
        {
            await runTask.ConfigureAwait(false);
        }

        cancellationTokenSource.Dispose();
        cancellationTokenSource = null;
        runTask = null;
        firstAttemptCompletion.TrySetResult();
    }

    //--------------------------------------------------------------------------------
    // Connection
    //--------------------------------------------------------------------------------

    // 受信メッセージの変換。タイムスタンプはUnixミリ秒からローカル時刻へ(recordのコンストラクター引数はConverterで変換する)
    [Mapper]
    [MapProperty(nameof(ChatMessageEntry.Timestamp), nameof(ChatMessage.Timestamp), Converter = nameof(ToLocalTime))]
    private static partial ChatMessageEntry ToEntry(ChatMessage message);

    private static DateTime ToLocalTime(long timestamp) =>
        DateTimeOffset.FromUnixTimeMilliseconds(timestamp).LocalDateTime;

    // 接続・受信・再接続のメインループ
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var connectedOnce = false;
        var delay = InitialRetryDelay;

        while (!cancellationToken.IsCancellationRequested)
        {
            SetState(connectedOnce ? ChatConnectionState.Reconnecting : ChatConnectionState.Connecting);
            try
            {
                using var channel = GrpcChannel.ForAddress(grpcUrl);
                var client = new ChatRoom.ChatRoomClient(channel);
                using var call = client.Connect(cancellationToken: cancellationToken);

                // レスポンスヘッダー受信をもって接続確立とみなす
                await call.ResponseHeadersAsync.WaitAsync(cancellationToken).ConfigureAwait(false);
                SetState(ChatConnectionState.Connected);
                connectedOnce = true;
                delay = InitialRetryDelay;
                firstAttemptCompletion.TrySetResult();

                // 送信ループを並走させる(受信終了時に停止)
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var sendTask = SendLoopAsync(call.RequestStream, linkedCts.Token);
                try
                {
                    // 受信ループ(切断時は例外終了して再接続へ)
                    await foreach (var message in call.ResponseStream.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                    {
                        MessageReceived?.Invoke(this, new ChatMessageEventArgs(ToEntry(message)));
                    }
                }
                finally
                {
                    await linkedCts.CancelAsync().ConfigureAwait(false);
                    await sendTask.ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (IsConnectionException(ex))
            {
                // 接続失敗・切断は再接続へ(モバイル回線の断続を想定)
            }

            firstAttemptCompletion.TrySetResult();

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // 指数バックオフで再接続
            SetState(ChatConnectionState.Reconnecting);
            try
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxRetryDelay.Ticks));
        }

        SetState(ChatConnectionState.Disconnected);
    }

    // 送信ループ(書き込み成功までキューから取り出さない: 失敗分は再接続後に再送される)
    private async Task SendLoopAsync(IClientStreamWriter<ChatMessage> writer, CancellationToken cancellationToken)
    {
        try
        {
            while (await sendChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (sendChannel.Reader.TryPeek(out var text))
                {
                    await writer.WriteAsync(new ChatMessage { User = userId, Text = text }, cancellationToken).ConfigureAwait(false);
                    sendChannel.Reader.TryRead(out _);
                }
            }
        }
        catch (Exception ex) when (IsConnectionException(ex))
        {
            // 切断時は終了(未送信分はキューに残る)
        }
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private void SetState(ChatConnectionState state)
    {
        if (State != state)
        {
            State = state;
            StateChanged?.Invoke(this, new ChatStateEventArgs(state));
        }
    }

    // 再接続対象の例外か
    private static bool IsConnectionException(Exception ex) =>
        ex is RpcException or HttpRequestException or IOException or SocketException or OperationCanceledException or InvalidOperationException;
}
