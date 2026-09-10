// ReSharper disable StringLiteralTypo
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Template_MobileServer_Web>("mobileserver")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
