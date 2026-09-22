namespace Template.MobileServer.Web.Services;

public sealed record ChatEntry(string User, string Text, DateTimeOffset Timestamp);

public sealed class ChatEntryEventArgs : EventArgs
{
    public ChatEntry Entry { get; }

    public ChatEntryEventArgs(ChatEntry entry)
    {
        Entry = entry;
    }
}

public sealed class ChatService
{
    private const int HistorySize = 50;

    private readonly Queue<ChatEntry> history = new();

    public event EventHandler<ChatEntryEventArgs>? Received;

    public IReadOnlyList<ChatEntry> History
    {
        get
        {
            lock (history)
            {
                return history.ToArray();
            }
        }
    }

    public void Publish(string user, string text, DateTimeOffset timestamp)
    {
        var entry = new ChatEntry(user, text, timestamp);
        lock (history)
        {
            history.Enqueue(entry);
            while (history.Count > HistorySize)
            {
                history.Dequeue();
            }
        }

        Received?.Invoke(this, new ChatEntryEventArgs(entry));
    }
}
