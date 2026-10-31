namespace Template.MobileServer.Models.Results;

public sealed record PagedResult<T>(
    int Total,
    int Page,
    int Size,
    IReadOnlyList<T> Items);
