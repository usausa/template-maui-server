namespace Template.MobileServer.Models.Views;

// 系列ごと・時間の束ごとの点の集計 (束の始まりは UTC の Unix ナノ秒)。Duration は点の区間 (開始から時刻まで) の長さの合計、
// Count / Sum / Max はヒストグラムとサマリー、LastUnixNano は束の中の最後の点の時刻
public sealed record TelemetryMetricBucketView(
    long SeriesId,
    long BucketUnixNano,
    int Points,
    int ValueCount,
    double? TotalValue,
    long Duration,
    long? Count,
    double? Sum,
    double? Max,
    long LastUnixNano);
