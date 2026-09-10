namespace Template.MobileServer.Web.Settings;

public sealed class JwtSetting
{
    [Required]
    public string Issuer { get; set; } = default!;

    [Required]
    public string Audience { get; set; } = default!;

    [Required]
    [MinLength(32)]
    public string SecretKey { get; set; } = default!;

    [Range(1, 1440)]
    public int ExpireMinutes { get; set; }
}
