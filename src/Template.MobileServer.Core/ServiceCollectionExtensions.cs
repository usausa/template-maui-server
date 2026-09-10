namespace Template.MobileServer;

using BunnyTail.ServiceRegistration;

using Microsoft.Extensions.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    [ServiceRegistration(Lifetime.Singleton, "Service$")]
    [ServiceRegistration(Lifetime.Singleton, "Usecase$")]
    public static partial IServiceCollection AddCoreServices(this IServiceCollection services);
}
