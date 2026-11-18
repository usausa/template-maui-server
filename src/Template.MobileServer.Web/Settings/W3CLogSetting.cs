namespace Template.MobileServer.Web.Settings;

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
