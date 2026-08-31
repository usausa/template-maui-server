namespace Template.MobileServer.Web.Settings;

#pragma warning disable CA1034
public sealed class ProfilerSetting
{
    [Required]
    public SqlLogSetting SqlLog { get; set; } = default!;

    [Required]
    public SqlTelemetrySetting SqlTelemetry { get; set; } = default!;

    public sealed class SqlLogSetting
    {
        public bool Enable { get; set; }

        public bool OutputParameter { get; set; } = true;

        [Range(0, 60_000)]
        public int ElapsedThresholdMilliseconds { get; set; }
    }

    public sealed class SqlTelemetrySetting
    {
        public bool Enable { get; set; }
    }
}
#pragma warning restore CA1034
