namespace Template.MobileServer.Domain;

// OTLP の SeverityNumber の区切り (TRACE = 1〜4、DEBUG = 5〜8、INFO = 9〜12、WARN = 13〜16、ERROR = 17〜20、FATAL = 21〜24)
public static class TelemetrySeverity
{
    public const int Trace = 1;

    public const int Debug = 5;

    public const int Info = 9;

    public const int Warn = 13;

    public const int Error = 17;

    public const int Fatal = 21;
}
