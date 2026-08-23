namespace Template.MobileServer.Models;

public sealed record PagedResult<T>(int Total, int Page, int Size, IReadOnlyList<T> Items);
