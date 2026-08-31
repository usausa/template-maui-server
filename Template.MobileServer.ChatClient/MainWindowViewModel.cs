namespace Template.MobileServer.ChatClient;

using Template.MobileServer.ChatClient.Chat;

// チャット画面のViewModel(WPF型に依存しない。MAUIではExtendViewModelBaseの読み替えのみで移植可能)
// [MEMO] UIスレッドへのマーシャリングはDispatcherではなくSynchronizationContextで吸収する
[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
internal sealed partial class MainWindowViewModel : ExtendViewModelBase, IAsyncDisposable
{
    private readonly SynchronizationContext? synchronizationContext;

    private Chat.ChatClient? client;

    [ObservableProperty]
    public partial string ServerUrl { get; set; }

    [ObservableProperty]
    public partial string GrpcUrl { get; set; }

    [ObservableProperty]
    public partial string UserId { get; set; }

    [ObservableProperty]
    public partial string Input { get; set; }

    [ObservableProperty]
    public partial ChatConnectionState State { get; set; }

    [ObservableProperty]
    public partial bool CanConnect { get; set; }

    [ObservableProperty]
    public partial bool CanDisconnect { get; set; }

    public ObservableCollection<ChatMessageEntry> Messages { get; } = [];

    public ICommand ConnectCommand { get; }

    public ICommand DisconnectCommand { get; }

    public ICommand SendCommand { get; }

    public MainWindowViewModel()
    {
        // ViewModel生成スレッド(UIスレッド)のコンテキストを保持する
        synchronizationContext = SynchronizationContext.Current;

        ServerUrl = "http://localhost:8081/";
        GrpcUrl = "http://localhost:8084/";
        UserId = "user";
        Input = string.Empty;
        State = ChatConnectionState.Disconnected;
        CanConnect = true;
        CanDisconnect = false;

        ConnectCommand = MakeAsyncCommand(ConnectAsync);
        DisconnectCommand = MakeAsyncCommand(DisconnectAsync);
        SendCommand = MakeAsyncCommand(SendAsync);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(true);
        Dispose();
    }

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private async Task ConnectAsync()
    {
        if ((client is not null) ||
            String.IsNullOrWhiteSpace(ServerUrl) ||
            String.IsNullOrWhiteSpace(GrpcUrl) ||
            String.IsNullOrWhiteSpace(UserId))
        {
            return;
        }

        var chatClient = new Chat.ChatClient(ServerUrl.Trim(), GrpcUrl.Trim(), UserId.Trim());
        chatClient.MessageReceived += OnMessageReceived;
        chatClient.StateChanged += OnStateChanged;
        client = chatClient;
        CanConnect = false;
        CanDisconnect = true;

        await chatClient.ConnectAsync().ConfigureAwait(true);
    }

    private async Task DisconnectAsync()
    {
        if (client is null)
        {
            return;
        }

        var chatClient = client;
        client = null;

        await chatClient.DisposeAsync().ConfigureAwait(true);

        chatClient.MessageReceived -= OnMessageReceived;
        chatClient.StateChanged -= OnStateChanged;
        CanConnect = true;
        CanDisconnect = false;
        State = ChatConnectionState.Disconnected;
    }

    private async Task SendAsync()
    {
        var text = Input.Trim();
        if ((client is null) || (text.Length == 0))
        {
            return;
        }

        await client.SendAsync(text).ConfigureAwait(true);
        Input = string.Empty;
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void OnMessageReceived(object? sender, ChatMessageEventArgs e) =>
        PostToUi(() => Messages.Add(e.Entry));

    private void OnStateChanged(object? sender, ChatStateEventArgs e) =>
        PostToUi(() => State = e.State);

    // UIスレッドへのマーシャリング(WPF/MAUIともに動作する)
    private void PostToUi(Action action)
    {
        if ((synchronizationContext is null) || (SynchronizationContext.Current == synchronizationContext))
        {
            action();
        }
        else
        {
            synchronizationContext.Post(static state => ((Action)state!)(), action);
        }
    }
}
