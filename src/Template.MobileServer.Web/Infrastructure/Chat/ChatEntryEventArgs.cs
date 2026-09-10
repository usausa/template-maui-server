namespace Template.MobileServer.Web.Infrastructure.Chat;

public sealed class ChatEntryEventArgs : EventArgs
{
    public ChatEntry Entry { get; }

    public ChatEntryEventArgs(ChatEntry entry)
    {
        Entry = entry;
    }
}
