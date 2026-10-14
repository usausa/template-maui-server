namespace Template.MobileServer.Models.Views;

public sealed record PushPendingSummaryView(
    string DeviceId,
    int Count);
