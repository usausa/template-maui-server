namespace Template.MobileServer.Web.Models.Api;

// [配置区分] Models/Api: モバイル契約DTO(PascalCaseのJSON契約)
public sealed class SecretMessageResponse
{
    public string Message { get; set; } = default!;
}
