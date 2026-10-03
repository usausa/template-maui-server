namespace Template.MobileServer.Models.Views;

// 端末ごとの未達の通知の件数
public sealed record PushPendingSummaryView(
    string DeviceId,
    int Count);
