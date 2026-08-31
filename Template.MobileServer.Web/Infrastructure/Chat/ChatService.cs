namespace Template.MobileServer.Web.Infrastructure.Chat;

// チャットのブロードキャストと直近履歴を管理する(プロセス内、gRPC/Blazor共用)
public sealed class ChatService
{
    private const int HistorySize = 50;

    private readonly Queue<ChatEntry> history = new();

    public event EventHandler<ChatEntryEventArgs>? Received;

    // 直近の履歴(スナップショット)
    public IReadOnlyList<ChatEntry> History
    {
        get
        {
            lock (history)
            {
                return [.. history];
            }
        }
    }

    // 発言を履歴へ追加し全参加者へ配信する
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
