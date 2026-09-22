namespace Template.MobileServer.Models;

public sealed record RangeResult<T>(int Total, int Offset, int Size, IReadOnlyList<T> Items);
