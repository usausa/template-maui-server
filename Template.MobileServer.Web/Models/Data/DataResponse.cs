namespace Template.MobileServer.Web.Models.Data;

// [配置区分] Models/Data: 管理DTO(Data管理API用)
public sealed record DataResponse(long Id, string Name, int Value, DateTime CreatedAt);
