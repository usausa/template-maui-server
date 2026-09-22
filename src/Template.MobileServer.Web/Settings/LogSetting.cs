namespace Template.MobileServer.Web.Settings;

#pragma warning disable CA1034
public sealed class LogSetting
{
    public bool HttpLog { get; set; }

    public bool HttpDump { get; set; }

    [Range(1, 1_048_576)]
    public int HttpDumpLimit { get; set; }

    [Required]
    public W3CLogSetting W3CLog { get; set; } = default!;

    public sealed class W3CLogSetting
    {
        public bool Enable { get; set; }

        [Required]
        public string Directory { get; set; } = default!;

        [Required]
        public string FileName { get; set; } = default!;

        [Range(1, 1000)]
        public int RetainedFileCount { get; set; }
    }
}
#pragma warning restore CA1034
