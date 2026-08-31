namespace Template.MobileServer.ChatClient.Chat;

internal sealed class ChatStateEventArgs : EventArgs
{
    public ChatConnectionState State { get; }

    public ChatStateEventArgs(ChatConnectionState state)
    {
        State = state;
    }
}
