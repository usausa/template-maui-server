namespace Template.MobileServer.Web.Infrastructure.Chat;

// チャットの発言(履歴・配信用)
public sealed record ChatEntry(string User, string Text, DateTimeOffset Timestamp);
