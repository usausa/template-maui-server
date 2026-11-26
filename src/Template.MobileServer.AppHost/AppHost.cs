// ReSharper disable StringLiteralTypo
var builder = DistributedApplication.CreateBuilder(args);

builder.AddDotnetProject("mobileserver", "../Template.MobileServer.Web/Template.MobileServer.Web.csproj")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
