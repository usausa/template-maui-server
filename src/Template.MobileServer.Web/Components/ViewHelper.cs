namespace Template.MobileServer.Web.Components;

using MudBlazor;

using Template.MobileServer.Infrastructure.Storage;
using Template.MobileServer.Web.Telemetry;

// 値の良し悪し (色分け)
public enum TelemetryLevel
{
    None,
    Good,
    Warning,
    Critical
}

public static class ViewHelper
{
    // 電池の残量の区切り (これ未満で Warning / Critical)
    public const double BatteryWarningRatio = 0.5;
    public const double BatteryCriticalRatio = 0.2;

    private const long NanosecondsPerMillisecond = 1_000_000;

    //--------------------------------------------------------------------------------
    // Format
    //--------------------------------------------------------------------------------

    public static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        < 1024L * 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F1} GB",
        _ => $"{bytes / (1024.0 * 1024 * 1024 * 1024):F2} TB"
    };

    public static string FormatSize(StorageEntry entry) =>
        entry.IsDirectory ? string.Empty : FormatBytes(entry.Size);

    public static string FormatTimestamp(DateTime value) =>
        value.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

    //--------------------------------------------------------------------------------
    // Icon
    //--------------------------------------------------------------------------------

    private static readonly HashSet<string> ImageExtensions =
        [with([".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".svg", ".ico"], StringComparer.OrdinalIgnoreCase)];

    private static readonly HashSet<string> VideoExtensions =
        [with([".mp4", ".avi", ".mov", ".wmv", ".mkv", ".webm"], StringComparer.OrdinalIgnoreCase)];

    private static readonly HashSet<string> AudioExtensions =
        [with([".mp3", ".wav", ".ogg", ".flac", ".aac", ".m4a"], StringComparer.OrdinalIgnoreCase)];

    private static readonly HashSet<string> ArchiveExtensions =
        [with([".zip", ".rar", ".7z", ".tar", ".gz"], StringComparer.OrdinalIgnoreCase)];

    private static readonly HashSet<string> TextExtensions =
        [with([".txt", ".log", ".md", ".csv"], StringComparer.OrdinalIgnoreCase)];

    private static readonly HashSet<string> CodeExtensions =
    [
        with([".cs", ".js", ".ts", ".py", ".java", ".cpp", ".h", ".html", ".css", ".json", ".xml"], StringComparer.OrdinalIgnoreCase)
    ];

    public static string GetIcon(StorageEntry entry)
    {
        if (entry.IsDirectory)
        {
            return Icons.Material.Filled.Folder;
        }

        var ext = Path.GetExtension(entry.Name);
        if (ImageExtensions.Contains(ext))
        {
            return Icons.Material.Filled.Image;
        }

        if (VideoExtensions.Contains(ext))
        {
            return Icons.Material.Filled.Movie;
        }

        if (AudioExtensions.Contains(ext))
        {
            return Icons.Material.Filled.MusicNote;
        }

        if (ext == ".pdf")
        {
            return Icons.Material.Filled.PictureAsPdf;
        }

        if (ArchiveExtensions.Contains(ext))
        {
            return Icons.Material.Filled.Archive;
        }

        if (TextExtensions.Contains(ext))
        {
            return Icons.Material.Filled.Description;
        }

        if (CodeExtensions.Contains(ext))
        {
            return Icons.Material.Filled.Code;
        }

        return Icons.Material.Filled.InsertDriveFile;
    }

    //--------------------------------------------------------------------------------
    // Time
    //--------------------------------------------------------------------------------

    public static DateTimeOffset ToDateTimeOffset(long unixNano) =>
        DateTimeOffset.FromUnixTimeMilliseconds(unixNano / NanosecondsPerMillisecond);

    public static string FormatTime(long unixNano) =>
        ViewHelper.FormatTimestamp(ToDateTimeOffset(unixNano).LocalDateTime);

    // 所要時間 (ナノ秒)。1 ms 未満は µs、1 秒未満は ms、それ以上は秒
    public static string FormatDuration(long nanoseconds) => nanoseconds switch
    {
        0 => "0",
        < 1_000_000 => $"{nanoseconds / 1_000.0:0.#} µs",
        < 1_000_000_000 => $"{nanoseconds / 1_000_000.0:0.##} ms",
        _ => $"{nanoseconds / 1_000_000_000.0:0.##} s"
    };

    // いちばん大きい単位 (秒・分・時間・日) で切り捨て
    public static string FormatElapsed(TimeSpan elapsed) => elapsed switch
    {
        { TotalMinutes: < 1 } => $"{Math.Max((int)elapsed.TotalSeconds, 0)} 秒前",
        { TotalHours: < 1 } => $"{(int)elapsed.TotalMinutes} 分前",
        { TotalDays: < 1 } => $"{(int)elapsed.TotalHours} 時間前",
        _ => $"{(int)elapsed.TotalDays} 日前"
    };

    //--------------------------------------------------------------------------------
    // Value
    //--------------------------------------------------------------------------------

    // 割合 (0〜1)
    public static string FormatPercent(double ratio) => $"{ratio * 100:F0}%";

    // 使用率 (0〜1。小数 1 桁)
    public static string FormatUtilization(double ratio) => $"{ratio * 100:F1}%";

    public static string FormatSignal(double dbm) => $"{dbm:F0} dBm";

    // アプリケーション固有値 (0〜100)
    public static string FormatCustomValue(double value) => $"{value:F0}";

    // 桁に合わせて小数を減らす (グラフの値)
    public static string FormatNumber(double value) => Math.Abs(value) switch
    {
        >= 100 => value.ToString("N0", CultureInfo.CurrentCulture),
        >= 10 => value.ToString("0.#", CultureInfo.CurrentCulture),
        >= 1 => value.ToString("0.##", CultureInfo.CurrentCulture),
        _ => value.ToString("0.###", CultureInfo.CurrentCulture)
    };

    //--------------------------------------------------------------------------------
    // Level
    //--------------------------------------------------------------------------------

    public static TelemetryLevel BatteryLevel(double ratio) => ratio switch
    {
        < BatteryCriticalRatio => TelemetryLevel.Critical,
        < BatteryWarningRatio => TelemetryLevel.Warning,
        _ => TelemetryLevel.Good
    };

    // CPU: 50% 未満 = Good、80% 未満 = Warning
    public static TelemetryLevel UtilizationLevel(double ratio) => ratio switch
    {
        < 0.5 => TelemetryLevel.Good,
        < 0.8 => TelemetryLevel.Warning,
        _ => TelemetryLevel.Critical
    };

    // 無線 LAN の信号強度: -67 dBm 以上 = Good、-80 dBm 以上 = Warning (端末の診断パネルと同じ)
    public static TelemetryLevel SignalLevel(double dbm) => dbm switch
    {
        >= -67 => TelemetryLevel.Good,
        >= -80 => TelemetryLevel.Warning,
        _ => TelemetryLevel.Critical
    };

    // 電池のアイコン (残量の段階)
    public static string BatteryIcon(double ratio) => ratio switch
    {
        >= 0.95 => Icons.Material.Filled.BatteryFull,
        >= 0.8 => Icons.Material.Filled.Battery6Bar,
        >= 0.65 => Icons.Material.Filled.Battery5Bar,
        >= 0.5 => Icons.Material.Filled.Battery4Bar,
        >= 0.35 => Icons.Material.Filled.Battery3Bar,
        >= 0.2 => Icons.Material.Filled.Battery2Bar,
        >= 0.05 => Icons.Material.Filled.Battery1Bar,
        _ => Icons.Material.Filled.Battery0Bar
    };

    // 無線 LAN のアイコン (4 段)
    public static string SignalIcon(double dbm) => dbm switch
    {
        >= -55 => Icons.Material.Filled.SignalWifi4Bar,
        >= -67 => Icons.Material.Filled.NetworkWifi3Bar,
        >= -80 => Icons.Material.Filled.NetworkWifi2Bar,
        _ => Icons.Material.Filled.NetworkWifi1Bar
    };

    public static Color LevelColor(TelemetryLevel level) => level switch
    {
        TelemetryLevel.Good => Color.Success,
        TelemetryLevel.Warning => Color.Warning,
        TelemetryLevel.Critical => Color.Error,
        _ => Color.Default
    };

    // 横棒の塗り (app.css)
    public static string LevelClass(TelemetryLevel level) => level switch
    {
        TelemetryLevel.Good => "meter-good",
        TelemetryLevel.Warning => "meter-warning",
        TelemetryLevel.Critical => "meter-critical",
        _ => "meter-none"
    };

    //--------------------------------------------------------------------------------
    // State
    //--------------------------------------------------------------------------------

    public static string FormatState(TelemetryDeviceState state) => state switch
    {
        TelemetryDeviceState.Receiving => "受信中",
        TelemetryDeviceState.Stopped => "途絶",
        _ => "受信なし"
    };

    public static Color StateColor(TelemetryDeviceState state) => state switch
    {
        TelemetryDeviceState.Receiving => Color.Success,
        TelemetryDeviceState.Stopped => Color.Warning,
        _ => Color.Default
    };

    // 受信なしは枠だけ
    public static Variant StateVariant(TelemetryDeviceState state) =>
        state == TelemetryDeviceState.NoData ? Variant.Outlined : Variant.Filled;

    //--------------------------------------------------------------------------------
    // Severity
    //--------------------------------------------------------------------------------

    public static string FormatSeverity(int severityNumber) => severityNumber switch
    {
        >= TelemetrySeverity.Fatal => "FATAL",
        >= TelemetrySeverity.Error => "ERROR",
        >= TelemetrySeverity.Warn => "WARN",
        >= TelemetrySeverity.Info => "INFO",
        >= TelemetrySeverity.Debug => "DEBUG",
        >= TelemetrySeverity.Trace => "TRACE",
        _ => "-"
    };

    public static Color SeverityColor(int severityNumber) => severityNumber switch
    {
        >= TelemetrySeverity.Error => Color.Error,
        >= TelemetrySeverity.Warn => Color.Warning,
        >= TelemetrySeverity.Info => Color.Info,
        _ => Color.Default
    };
}
