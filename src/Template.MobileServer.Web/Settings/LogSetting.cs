namespace Template.MobileServer.Web.Settings;

public sealed class LogSetting
{
    public bool HttpLog { get; set; }

    public bool HttpDump { get; set; }

    [Range(1, 1_048_576)]
    public int HttpDumpLimit { get; set; }

    [Required]
    [ValidateObjectMembers]
    public W3CLogSetting W3CLog { get; set; } = default!;
}
