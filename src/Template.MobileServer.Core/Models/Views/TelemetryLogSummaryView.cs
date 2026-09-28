namespace Template.MobileServer.Models.Views;

// 1 時間 (Unix 時 = UTC の時刻 / 1 時間) ごとのエラー (ERROR 以上) とクラッシュ (FATAL) の件数
public sealed record TelemetryLogSummaryView(
    long Hour,
    int ErrorCount,
    int CrashCount);
