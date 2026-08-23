namespace Template.MobileServer.Web.Models.Data;

public sealed record DataCreateRequest(
    [property: Required][property: MaxLength(50)] string Name,
    [property: Range(0, 1_000_000)] int Value);
