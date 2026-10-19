namespace Template.MobileServer.Models.Views;

public sealed record TelemetryLogSummaryView(
    long Hour,
    int ErrorCount,
    int CrashCount);
