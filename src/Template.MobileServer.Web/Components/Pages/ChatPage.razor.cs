namespace Template.MobileServer.Web.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using MudBlazor;

using Template.MobileServer.Web.Services;

public sealed partial class ChatPage
{
    private readonly List<ChatEntry> entries = [];

    private string input = string.Empty;

    private string userName = "web";

    private bool scrollRequested;

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Inject]
    public required ChatService ChatService { get; set; }

    [Inject]
    public required TimeProvider TimeProvider { get; set; }

    [Inject]
    public required IScrollManager ScrollManager { get; set; }

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnInitialized()
    {
        entries.AddRange(ChatService.History);
        ChatService.Received += OnReceived;
        scrollRequested = entries.Count > 0;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
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

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void OnReceived(object? sender, ChatEntryEventArgs e)
    {
        _ = InvokeAsync(() =>
        {
            entries.Add(e.Entry);
            scrollRequested = true;
            StateHasChanged();
        });
    }

    private void OnInputKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            Send();
        }
    }

    //--------------------------------------------------------------------------------
    // Action
    //--------------------------------------------------------------------------------

    private void Send()
    {
        var text = input.Trim();
        if (text.Length == 0)
        {
            return;
        }

        ChatService.Publish(String.IsNullOrWhiteSpace(userName) ? "web" : userName.Trim(), text, TimeProvider.GetUtcNow());
        input = string.Empty;
    }
}
