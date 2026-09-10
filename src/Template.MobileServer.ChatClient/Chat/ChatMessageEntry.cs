namespace Template.MobileServer.ChatClient.Chat;

// チャットの発言(タイムスタンプはローカル時刻)
internal sealed record ChatMessageEntry(string User, string Text, DateTime Timestamp);
