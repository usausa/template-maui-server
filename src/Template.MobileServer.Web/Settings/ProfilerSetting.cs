namespace Template.MobileServer.Web.Settings;

public sealed class ProfilerSetting
{
    [Required]
    [ValidateObjectMembers]
    public SqlLogSetting SqlLog { get; set; } = default!;

    [Required]
    public SqlTelemetrySetting SqlTelemetry { get; set; } = default!;
}
