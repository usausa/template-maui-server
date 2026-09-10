namespace Template.MobileServer.Web.Models.Data;

// [配置区分] Models/Data: 管理DTO(Data管理API用)
public sealed record DataUpdateRequest(
    [property: Required][property: MaxLength(50)] string Name,
    [property: Range(0, 1_000_000)] int Value);
