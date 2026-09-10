namespace Template.MobileServer.Web.Settings;

public sealed class AuthSetting
{
    [Range(1, 43200)]
    public int ExpireMinutes { get; set; }

    [Required]
    public string InitialId { get; set; } = default!;

    [Required]
    public string InitialPassword { get; set; } = default!;
}
