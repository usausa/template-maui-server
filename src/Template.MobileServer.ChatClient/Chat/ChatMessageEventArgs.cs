namespace Template.MobileServer.ChatClient.Chat;

internal sealed class ChatMessageEventArgs : EventArgs
{
    public ChatMessageEntry Entry { get; }

    public ChatMessageEventArgs(ChatMessageEntry entry)
    {
        Entry = entry;
    }
}
