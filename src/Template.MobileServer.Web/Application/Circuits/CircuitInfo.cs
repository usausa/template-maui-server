namespace Template.MobileServer.Web.Application.Circuits;

public sealed record CircuitInfo(string Id, DateTimeOffset OpenedAt, bool Connected);
