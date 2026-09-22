namespace Template.MobileServer.Services;

using Template.MobileServer.Models;

public abstract class ServiceContextProvider
{
    public abstract ServiceContext Current { get; }
}
