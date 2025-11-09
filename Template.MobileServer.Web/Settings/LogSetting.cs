namespace Template.MobileServer.Web.Settings;

public sealed class LogSetting
{
    public sealed class W3CEntry
    {
        public string LogDirectory { get; set; } = default!;

        public string[]? AdditionalHeaders { get; set; }
    }

    public W3CEntry? W3C { get; set; }

    public sealed class HttpEntry
    {
        public bool HttpLogging { get; set; }

        public bool DumpBody { get; set; }
    }

    public HttpEntry? Http { get; set; }

    public sealed class DataEntry
    {
        public bool SqlTrace { get; set; }
    }

    public DataEntry? Data { get; set; }
}
