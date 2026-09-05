namespace Template.MobileServer.Web.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;

using MudBlazor;

using Template.MobileServer.Web.Infrastructure.Chat;

// チャットページ(gRPCを経由せずChatService直結でプロセス内イベントを購読)
public sealed partial class ChatPage
{
    private readonly List<ChatEntry> entries = [];

    private string input = string.Empty;

    private string userName = "unknown";

    private bool scrollRequested;

    [Inject]
    public required ChatService ChatService { get; set; }

    [Inject]
    public required TimeProvider TimeProvider { get; set; }

    [Inject]
    public required IScrollManager ScrollManager { get; set; }

    [CascadingParameter]
    public required Task<AuthenticationState> AuthenticationState { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // 送信者名はログインユーザー名
        var state = await AuthenticationState;
        userName = state.User.Identity?.Name ?? "unknown";

        // 履歴を表示して以降の発言を購読する(購読解除はDispose)
        entries.AddRange(ChatService.History);
        ChatService.Received += OnReceived;
        scrollRequested = entries.Count > 0;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // 新着時は末尾へ自動スクロール
        if (scrollRequested)
        {
            scrollRequested = false;
            await ScrollManager.ScrollToBottomAsync("#chat-messages");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ChatService.Received -= OnReceived;
        }

        base.Dispose(disposing);
    }

    private void OnReceived(object? sender, ChatEntryEventArgs e)
    {
        _ = InvokeAsync(() =>
        {
            entries.Add(e.Entry);
            scrollRequested = true;
            StateHasChanged();
        });
    }

    private void Send()
    {
        var text = input.Trim();
        if (text.Length == 0)
        {
            return;
        }

        ChatService.Publish(userName, text, TimeProvider.GetUtcNow());
        input = string.Empty;
    }

    private void OnInputKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            Send();
        }
    }
}
