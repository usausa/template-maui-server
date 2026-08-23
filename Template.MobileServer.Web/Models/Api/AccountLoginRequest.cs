namespace Template.MobileServer.Web.Models.Api;

public sealed class AccountLoginRequest
{
    [Required]
    public string Id { get; set; } = default!;
}
