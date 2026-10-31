namespace Template.MobileServer.Models.Results;

public sealed record RangeResult<T>(
    int Total,
    int Offset,
    int Size,
    IReadOnlyList<T> Items);
