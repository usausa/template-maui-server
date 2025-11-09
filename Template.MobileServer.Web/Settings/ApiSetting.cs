namespace Template.MobileServer.Web.Settings;

public sealed class ApiSetting
{
    public bool UseRequestDecompression { get; set; }

    public bool UseResponseCompression { get; set; }

    public class RateLimitEntry
    {
        public bool Enable { get; set; }

        public int Window { get; set; }

        public int PermitLimit { get; set; }

        public int QueueLimit { get; set; }
    }

    public RateLimitEntry? RateLimit { get; set; }
}
